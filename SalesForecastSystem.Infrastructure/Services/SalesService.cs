using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Sales;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Services;

public sealed class SalesService(AppDbContext context) : ISalesService
{
    public async Task<PagedResponse<SalesListItemResponse>> GetPagedAsync(SalesQueryRequest request, CancellationToken cancellationToken)
    {
        var query = context.SalesOrders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.OrderNumber.Contains(search)
                || (x.CustomerName != null && x.CustomerName.Contains(search))
                || (x.CustomerPhone != null && x.CustomerPhone.Contains(search)));
        }
        if (request.WarehouseId.HasValue) query = query.Where(x => x.WarehouseId == request.WarehouseId);
        if (request.CustomerId.HasValue) query = query.Where(x => x.CustomerId == request.CustomerId);
        if (request.CreatedByUserId.HasValue) query = query.Where(x => x.CreatedByUserId == request.CreatedByUserId);
        if (request.FromDate.HasValue) query = query.Where(x => x.OrderDate >= request.FromDate.Value.Date);
        if (request.ToDate.HasValue) query = query.Where(x => x.OrderDate <= request.ToDate.Value.Date);
        if (request.Status is not null) query = query.Where(x => x.Status == request.Status);
        var total = await query.CountAsync(cancellationToken);
        var skip = ((long)request.Page - 1) * request.PageSize;
        var items = skip >= total ? [] : await query.OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.SalesOrderId)
            .Skip((int)skip).Take(request.PageSize)
            .Select(x => new SalesListItemResponse(x.SalesOrderId, x.OrderNumber, x.WarehouseId,
                x.CustomerId, x.CreatedByUserId, x.OrderDate, x.Status, x.CreatedAt,
                x.CustomerName, x.CustomerPhone, x.ShippingAddress,
                x.Items.Sum(item => (decimal?)item.LineTotal) ?? 0m)).ToListAsync(cancellationToken);
        return new PagedResponse<SalesListItemResponse>(items, request.Page, request.PageSize, total,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize));
    }

    public async Task<ServiceResult<SalesResponse>> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        await ReadAsync(id, cancellationToken) is { } item ? ServiceResult<SalesResponse>.Success(item) : NotFound();

    public async Task<ServiceResult<SalesResponse>> CreateAsync(SalesRequest request, int userId, CancellationToken cancellationToken)
    {
        var error = await ValidateReferencesAsync(request, cancellationToken);
        if (error is not null) return error;
        var order = new SalesOrder { CreatedByUserId = userId };
        Apply(order, request);
        await SnapshotCustomerAsync(order, cancellationToken);
        context.SalesOrders.Add(order);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) { return DatabaseError(ex); }
        return (await GetByIdAsync(order.SalesOrderId, cancellationToken));
    }

    public async Task<ServiceResult<SalesResponse>> UpdateAsync(long id, SalesRequest request, CancellationToken cancellationToken)
    {
        var order = await context.SalesOrders.SingleOrDefaultAsync(x => x.SalesOrderId == id, cancellationToken);
        if (order is null) return NotFound();
        if (order.Status != "Draft") return Conflict("Only draft sales orders can be edited.");
        var error = await ValidateReferencesAsync(request, cancellationToken);
        if (error is not null) return error;
        Apply(order, request);
        await SnapshotCustomerAsync(order, cancellationToken);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Conflict("Sales order changed concurrently."); }
        catch (DbUpdateException ex) { return DatabaseError(ex); }
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ServiceResult<SalesResponse>> AddItemAsync(long id, SalesItemRequest request, CancellationToken cancellationToken)
    {
        var error = await CheckDraftAndProductAsync(id, request.ProductId, cancellationToken);
        if (error is not null) return error;
        context.SalesOrderItems.Add(new SalesOrderItem
        {
            SalesOrderId = id, ProductId = request.ProductId, Quantity = request.Quantity,
            UnitPrice = request.UnitPrice, Discount = request.Discount
        });
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) { return DatabaseError(ex); }
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ServiceResult<SalesResponse>> UpdateItemAsync(long id, long itemId, SalesItemRequest request, CancellationToken cancellationToken)
    {
        var error = await CheckDraftAndProductAsync(id, request.ProductId, cancellationToken);
        if (error is not null) return error;
        var item = await context.SalesOrderItems.SingleOrDefaultAsync(x => x.SalesOrderId == id && x.SalesOrderItemId == itemId, cancellationToken);
        if (item is null) return ServiceResult<SalesResponse>.Failure(ServiceErrorType.NotFound, "Sales item was not found.");
        item.ProductId = request.ProductId;
        item.Quantity = request.Quantity;
        item.UnitPrice = request.UnitPrice;
        item.Discount = request.Discount;
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Conflict("Sales item changed concurrently."); }
        catch (DbUpdateException ex) { return DatabaseError(ex); }
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ServiceResult<bool>> DeleteItemAsync(long id, long itemId, CancellationToken cancellationToken)
    {
        var order = await context.SalesOrders.AsNoTracking().SingleOrDefaultAsync(x => x.SalesOrderId == id, cancellationToken);
        if (order is null) return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Sales order was not found.");
        if (order.Status != "Draft") return ServiceResult<bool>.Failure(ServiceErrorType.Conflict, "Only draft sales orders can be edited.");
        var item = await context.SalesOrderItems.SingleOrDefaultAsync(x => x.SalesOrderId == id && x.SalesOrderItemId == itemId, cancellationToken);
        if (item is null) return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Sales item was not found.");
        context.SalesOrderItems.Remove(item);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return ServiceResult<bool>.Failure(ServiceErrorType.Conflict, "Sales order changed concurrently."); }
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<SalesResponse>> CompleteAsync(long id, CancellationToken cancellationToken)
    {
        var status = await context.SalesOrders.AsNoTracking().Where(x => x.SalesOrderId == id)
            .Select(x => x.Status).SingleOrDefaultAsync(cancellationToken);
        if (status is null) return NotFound();
        if (status != "Delivering" && status != "Completed")
            return Conflict("Only delivering sales orders can be completed.");
        try { await context.Database.ExecuteSqlInterpolatedAsync($"EXEC dbo.usp_CompleteSalesOrder @SalesOrderId={id}", cancellationToken); }
        catch (SqlException ex) when (ex.Number == 51101) { return NotFound(); }
        catch (SqlException ex) when (ex.Number is >= 51102 and <= 51107 or 51210)
        { return Conflict(ex.Message); }
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ServiceResult<SalesResponse>> SubmitAsync(long id, CancellationToken cancellationToken)
    {
        if (!await context.SalesOrders.AnyAsync(x => x.SalesOrderId == id, cancellationToken)) return NotFound();
        if (!await context.SalesOrderItems.AnyAsync(x => x.SalesOrderId == id, cancellationToken))
            return Conflict("Sales order has no items.");
        return await ChangeStatusAsync(id, "Draft", "Pending", cancellationToken);
    }

    public Task<ServiceResult<SalesResponse>> DispatchAsync(long id, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, "Pending", "Delivering", cancellationToken);

    public async Task<ServiceResult<SalesResponse>> CancelAsync(long id, CancellationToken cancellationToken)
    {
        var status = await context.SalesOrders.AsNoTracking().Where(x => x.SalesOrderId == id)
            .Select(x => x.Status).SingleOrDefaultAsync(cancellationToken);
        if (status is null) return NotFound();
        if (status == "Cancelled") return await GetByIdAsync(id, cancellationToken);
        if (status == "Completed") return Conflict("Completed sales orders cannot be cancelled.");
        return await ChangeStatusAsync(id, status, "Cancelled", cancellationToken);
    }

    private async Task<ServiceResult<SalesResponse>> ChangeStatusAsync(long id, string expected, string next, CancellationToken cancellationToken)
    {
        try
        {
            var changed = await context.SalesOrders.Where(x => x.SalesOrderId == id && x.Status == expected)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, next), cancellationToken);
            if (changed == 0)
                return await context.SalesOrders.AnyAsync(x => x.SalesOrderId == id, cancellationToken)
                    ? Conflict("Sales order status changed or transition is invalid.") : NotFound();
        }
        catch (SqlException ex) when (ex.Number is >= 51202 and <= 51204 or 51211 or 51212 or 547) { return Conflict(ex.Message); }
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var order = await context.SalesOrders.Include(x => x.Items).SingleOrDefaultAsync(x => x.SalesOrderId == id, cancellationToken);
        if (order is null) return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Sales order was not found.");
        if (order.Status != "Draft") return ServiceResult<bool>.Failure(ServiceErrorType.Conflict, "Only draft sales orders can be deleted.");
        context.SalesOrderItems.RemoveRange(order.Items);
        context.SalesOrders.Remove(order);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return ServiceResult<bool>.Failure(ServiceErrorType.Conflict, "Sales order changed concurrently."); }
        return ServiceResult<bool>.Success(true);
    }

    private async Task<ServiceResult<SalesResponse>?> ValidateReferencesAsync(SalesRequest request, CancellationToken cancellationToken)
    {
        if (!await context.Warehouses.AnyAsync(x => x.WarehouseId == request.WarehouseId && x.IsActive, cancellationToken))
            return ServiceResult<SalesResponse>.Failure(ServiceErrorType.Validation, "Active warehouse is required.", nameof(request.WarehouseId));
        if (request.CustomerId.HasValue &&
            !await context.Customers.AnyAsync(x => x.CustomerId == request.CustomerId && x.IsActive, cancellationToken))
            return ServiceResult<SalesResponse>.Failure(ServiceErrorType.Validation, "Active customer is required.", nameof(request.CustomerId));
        return null;
    }

    private async Task<ServiceResult<SalesResponse>?> CheckDraftAndProductAsync(long id, int productId, CancellationToken cancellationToken)
    {
        var status = await context.SalesOrders.AsNoTracking().Where(x => x.SalesOrderId == id).Select(x => x.Status).SingleOrDefaultAsync(cancellationToken);
        if (status is null) return NotFound();
        if (status != "Draft") return Conflict("Only draft sales orders can be edited.");
        if (!await context.Products.AnyAsync(x => x.ProductId == productId && x.IsActive, cancellationToken))
            return ServiceResult<SalesResponse>.Failure(ServiceErrorType.Validation, "Active product is required.", nameof(SalesItemRequest.ProductId));
        return null;
    }

    private async Task<SalesResponse?> ReadAsync(long id, CancellationToken cancellationToken)
    {
        var order = await context.SalesOrders.AsNoTracking().Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.SalesOrderId == id, cancellationToken);
        if (order is null) return null;
        var items = order.Items.OrderBy(x => x.SalesOrderItemId)
            .Select(x => new SalesItemResponse(x.SalesOrderItemId, x.ProductId, x.Quantity, x.UnitPrice, x.Discount, x.LineTotal)).ToList();
        return new SalesResponse(order.SalesOrderId, order.OrderNumber, order.WarehouseId, order.CustomerId,
            order.CreatedByUserId, order.OrderDate, order.Status, order.Notes,
            order.CustomerName, order.CustomerPhone, order.ShippingAddress, order.CreatedAt, order.CompletedAt,
            items.Sum(x => x.LineTotal), items);
    }

    private static void Apply(SalesOrder order, SalesRequest request)
    {
        order.OrderNumber = request.OrderNumber.Trim();
        order.WarehouseId = request.WarehouseId;
        order.CustomerId = request.CustomerId;
        order.OrderDate = request.OrderDate.Date;
        order.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        order.ShippingAddress = string.IsNullOrWhiteSpace(request.ShippingAddress) ? null : request.ShippingAddress.Trim();
    }

    private async Task SnapshotCustomerAsync(SalesOrder order, CancellationToken cancellationToken)
    {
        if (!order.CustomerId.HasValue)
        {
            order.CustomerName = null;
            order.CustomerPhone = null;
            return;
        }
        var customer = await context.Customers.AsNoTracking()
            .Where(x => x.CustomerId == order.CustomerId)
            .Select(x => new { x.FullName, x.PhoneNumber, x.Address })
            .SingleAsync(cancellationToken);
        order.CustomerName = customer.FullName;
        order.CustomerPhone = customer.PhoneNumber;
        order.ShippingAddress ??= customer.Address;
    }

    private static ServiceResult<SalesResponse> DatabaseError(DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sql)
        {
            if (sql.Number is 2601 or 2627) return Conflict("Sales order number or product already exists in this order.");
            if (sql.Number is >= 51202 and <= 51204) return Conflict("Sales order changed concurrently.");
            if (sql.Number == 547) return Conflict("Referenced record changed concurrently.");
        }
        throw ex;
    }
    private static ServiceResult<SalesResponse> NotFound() => ServiceResult<SalesResponse>.Failure(ServiceErrorType.NotFound, "Sales order was not found.");
    private static ServiceResult<SalesResponse> Conflict(string message) => ServiceResult<SalesResponse>.Failure(ServiceErrorType.Conflict, message);
}

