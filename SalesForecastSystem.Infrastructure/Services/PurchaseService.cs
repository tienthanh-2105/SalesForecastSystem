using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Purchases;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Services;

public sealed class PurchaseService(AppDbContext context) : IPurchaseService
{
    public async Task<PagedResponse<PurchaseListItemResponse>> GetPagedAsync(PurchaseQueryRequest request, CancellationToken cancellationToken)
    {
        var query = context.PurchaseOrders.AsNoTracking().AsQueryable();
        if (request.WarehouseId.HasValue) query = query.Where(x => x.WarehouseId == request.WarehouseId);
        if (request.SupplierId.HasValue) query = query.Where(x => x.SupplierId == request.SupplierId);
        if (request.FromDate.HasValue) query = query.Where(x => x.OrderDate >= request.FromDate.Value.Date);
        if (request.ToDate.HasValue) query = query.Where(x => x.OrderDate <= request.ToDate.Value.Date);
        if (request.Status is not null) query = query.Where(x => x.Status == request.Status);
        var total = await query.CountAsync(cancellationToken);
        var skip = ((long)request.Page - 1) * request.PageSize;
        var items = skip >= total ? [] : await query.OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.PurchaseOrderId)
            .Skip((int)skip).Take(request.PageSize)
            .Select(x => new PurchaseListItemResponse(x.PurchaseOrderId, x.OrderNumber, x.WarehouseId,
                x.SupplierId, x.OrderDate, x.Status, x.CreatedAt)).ToListAsync(cancellationToken);
        return new PagedResponse<PurchaseListItemResponse>(items, request.Page, request.PageSize, total,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize));
    }

    public async Task<ServiceResult<PurchaseResponse>> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        await ReadAsync(id, cancellationToken) is { } item ? ServiceResult<PurchaseResponse>.Success(item) : NotFound();

    public async Task<ServiceResult<PurchaseResponse>> CreateAsync(PurchaseRequest request, int userId, CancellationToken cancellationToken)
    {
        var error = await ValidateReferencesAsync(request, cancellationToken);
        if (error is not null) return error;
        var order = new PurchaseOrder { CreatedByUserId = userId };
        Apply(order, request);
        context.PurchaseOrders.Add(order);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) { return DatabaseError(ex); }
        return (await GetByIdAsync(order.PurchaseOrderId, cancellationToken));
    }

    public async Task<ServiceResult<PurchaseResponse>> UpdateAsync(long id, PurchaseRequest request, CancellationToken cancellationToken)
    {
        var order = await context.PurchaseOrders.SingleOrDefaultAsync(x => x.PurchaseOrderId == id, cancellationToken);
        if (order is null) return NotFound();
        if (order.Status != "Draft") return Conflict("Only draft purchase orders can be edited.");
        var error = await ValidateReferencesAsync(request, cancellationToken);
        if (error is not null) return error;
        Apply(order, request);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Conflict("Purchase order changed concurrently."); }
        catch (DbUpdateException ex) { return DatabaseError(ex); }
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ServiceResult<PurchaseResponse>> AddItemAsync(long id, PurchaseItemRequest request, CancellationToken cancellationToken)
    {
        var error = await CheckDraftAndProductAsync(id, request.ProductId, cancellationToken);
        if (error is not null) return error;
        context.PurchaseOrderItems.Add(new PurchaseOrderItem
        {
            PurchaseOrderId = id, ProductId = request.ProductId, Quantity = request.Quantity, UnitPrice = request.UnitPrice
        });
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) { return DatabaseError(ex); }
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ServiceResult<PurchaseResponse>> UpdateItemAsync(long id, long itemId, PurchaseItemRequest request, CancellationToken cancellationToken)
    {
        var error = await CheckDraftAndProductAsync(id, request.ProductId, cancellationToken);
        if (error is not null) return error;
        var item = await context.PurchaseOrderItems.SingleOrDefaultAsync(x => x.PurchaseOrderId == id && x.PurchaseOrderItemId == itemId, cancellationToken);
        if (item is null) return ServiceResult<PurchaseResponse>.Failure(ServiceErrorType.NotFound, "Purchase item was not found.");
        item.ProductId = request.ProductId;
        item.Quantity = request.Quantity;
        item.UnitPrice = request.UnitPrice;
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Conflict("Purchase item changed concurrently."); }
        catch (DbUpdateException ex) { return DatabaseError(ex); }
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ServiceResult<bool>> DeleteItemAsync(long id, long itemId, CancellationToken cancellationToken)
    {
        var order = await context.PurchaseOrders.AsNoTracking().SingleOrDefaultAsync(x => x.PurchaseOrderId == id, cancellationToken);
        if (order is null) return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Purchase order was not found.");
        if (order.Status != "Draft") return ServiceResult<bool>.Failure(ServiceErrorType.Conflict, "Only draft purchase orders can be edited.");
        var item = await context.PurchaseOrderItems.SingleOrDefaultAsync(x => x.PurchaseOrderId == id && x.PurchaseOrderItemId == itemId, cancellationToken);
        if (item is null) return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Purchase item was not found.");
        context.PurchaseOrderItems.Remove(item);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return ServiceResult<bool>.Failure(ServiceErrorType.Conflict, "Purchase order changed concurrently."); }
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<PurchaseResponse>> PostAsync(long id, CancellationToken cancellationToken)
    {
        try { await context.Database.ExecuteSqlInterpolatedAsync($"EXEC dbo.usp_PostPurchaseOrder @PurchaseOrderId={id}", cancellationToken); }
        catch (SqlException ex) when (ex.Number == 51001) { return NotFound(); }
        catch (SqlException ex) when (ex.Number is >= 51002 and <= 51006 or 51210)
        { return Conflict(ex.Message); }
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ServiceResult<PurchaseResponse>> CancelAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            var changed = await context.PurchaseOrders.Where(x => x.PurchaseOrderId == id && x.Status == "Draft")
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, "Cancelled"), cancellationToken);
            if (changed == 0)
            {
                var status = await context.PurchaseOrders.AsNoTracking().Where(x => x.PurchaseOrderId == id)
                    .Select(x => x.Status).SingleOrDefaultAsync(cancellationToken);
                return status is null ? NotFound() : status == "Cancelled"
                    ? await GetByIdAsync(id, cancellationToken) : Conflict("Only draft purchase orders can be cancelled.");
            }
        }
        catch (SqlException ex) when (ex.Number is 51201 or 547) { return Conflict(ex.Message); }
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var order = await context.PurchaseOrders.Include(x => x.Items).SingleOrDefaultAsync(x => x.PurchaseOrderId == id, cancellationToken);
        if (order is null) return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Purchase order was not found.");
        if (order.Status != "Draft") return ServiceResult<bool>.Failure(ServiceErrorType.Conflict, "Only draft purchase orders can be deleted.");
        context.PurchaseOrderItems.RemoveRange(order.Items);
        context.PurchaseOrders.Remove(order);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return ServiceResult<bool>.Failure(ServiceErrorType.Conflict, "Purchase order changed concurrently."); }
        return ServiceResult<bool>.Success(true);
    }

    private async Task<ServiceResult<PurchaseResponse>?> ValidateReferencesAsync(PurchaseRequest request, CancellationToken cancellationToken)
    {
        if (!await context.Warehouses.AnyAsync(x => x.WarehouseId == request.WarehouseId && x.IsActive, cancellationToken))
            return ServiceResult<PurchaseResponse>.Failure(ServiceErrorType.Validation, "Active warehouse is required.", nameof(request.WarehouseId));
        if (!await context.Suppliers.AnyAsync(x => x.SupplierId == request.SupplierId && x.IsActive, cancellationToken))
            return ServiceResult<PurchaseResponse>.Failure(ServiceErrorType.Validation, "Active supplier is required.", nameof(request.SupplierId));
        return null;
    }

    private async Task<ServiceResult<PurchaseResponse>?> CheckDraftAndProductAsync(long id, int productId, CancellationToken cancellationToken)
    {
        var status = await context.PurchaseOrders.AsNoTracking().Where(x => x.PurchaseOrderId == id).Select(x => x.Status).SingleOrDefaultAsync(cancellationToken);
        if (status is null) return NotFound();
        if (status != "Draft") return Conflict("Only draft purchase orders can be edited.");
        if (!await context.Products.AnyAsync(x => x.ProductId == productId && x.IsActive, cancellationToken))
            return ServiceResult<PurchaseResponse>.Failure(ServiceErrorType.Validation, "Active product is required.", nameof(PurchaseItemRequest.ProductId));
        return null;
    }

    private async Task<PurchaseResponse?> ReadAsync(long id, CancellationToken cancellationToken)
    {
        var order = await context.PurchaseOrders.AsNoTracking().Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.PurchaseOrderId == id, cancellationToken);
        if (order is null) return null;
        var items = order.Items.OrderBy(x => x.PurchaseOrderItemId)
            .Select(x => new PurchaseItemResponse(x.PurchaseOrderItemId, x.ProductId, x.Quantity, x.UnitPrice, x.LineTotal)).ToList();
        return new PurchaseResponse(order.PurchaseOrderId, order.OrderNumber, order.WarehouseId, order.SupplierId,
            order.CreatedByUserId, order.OrderDate, order.Status, order.Notes, order.CreatedAt, order.PostedAt,
            items.Sum(x => x.LineTotal), items);
    }

    private static void Apply(PurchaseOrder order, PurchaseRequest request)
    {
        order.OrderNumber = request.OrderNumber.Trim();
        order.WarehouseId = request.WarehouseId;
        order.SupplierId = request.SupplierId;
        order.OrderDate = request.OrderDate.Date;
        order.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
    }

    private static ServiceResult<PurchaseResponse> DatabaseError(DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sql)
        {
            if (sql.Number is 2601 or 2627) return Conflict("Purchase order number or product already exists in this order.");
            if (sql.Number is >= 51201 and <= 51203) return Conflict("Purchase order changed concurrently.");
            if (sql.Number == 547) return Conflict("Referenced record changed concurrently.");
        }
        throw ex;
    }
    private static ServiceResult<PurchaseResponse> NotFound() => ServiceResult<PurchaseResponse>.Failure(ServiceErrorType.NotFound, "Purchase order was not found.");
    private static ServiceResult<PurchaseResponse> Conflict(string message) => ServiceResult<PurchaseResponse>.Failure(ServiceErrorType.Conflict, message);
}
