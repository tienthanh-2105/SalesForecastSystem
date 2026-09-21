using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Warehouses;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Services;

public sealed class WarehouseService(AppDbContext context) : IWarehouseService
{
    public async Task<PagedResponse<WarehouseResponse>> GetPagedAsync(WarehouseQueryRequest request, CancellationToken cancellationToken = default)
    {
        var query = context.Warehouses.AsNoTracking().AsQueryable();
        var search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search)) query = query.Where(item => item.Name.Contains(search) || (item.Address != null && item.Address.Contains(search)));
        if (request.IsActive.HasValue) query = query.Where(item => item.IsActive == request.IsActive.Value);
        var total = await query.CountAsync(cancellationToken);
        var skip = ((long)request.Page - 1) * request.PageSize;
        var items = skip >= total ? [] : await query.OrderBy(item => item.Name).ThenBy(item => item.WarehouseId)
            .Skip((int)skip).Take(request.PageSize).Select(item => new WarehouseResponse(item.WarehouseId, item.Name, item.Address, item.IsActive))
            .ToListAsync(cancellationToken);
        return new PagedResponse<WarehouseResponse>(items, request.Page, request.PageSize, total,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize));
    }

    public async Task<ServiceResult<WarehouseResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await context.Warehouses.AsNoTracking().SingleOrDefaultAsync(x => x.WarehouseId == id, cancellationToken);
        return item is null ? NotFound() : ServiceResult<WarehouseResponse>.Success(ToResponse(item));
    }

    public async Task<ServiceResult<WarehouseResponse>> CreateAsync(WarehouseRequest request, CancellationToken cancellationToken = default)
    {
        var item = new Warehouse();
        Apply(item, request);
        context.Warehouses.Add(item);
        var error = await SaveAsync(cancellationToken);
        return error ?? ServiceResult<WarehouseResponse>.Success(ToResponse(item));
    }

    public async Task<ServiceResult<WarehouseResponse>> UpdateAsync(int id, WarehouseRequest request, CancellationToken cancellationToken = default)
    {
        var item = await context.Warehouses.SingleOrDefaultAsync(x => x.WarehouseId == id, cancellationToken);
        if (item is null) return NotFound();
        Apply(item, request);
        var error = await SaveAsync(cancellationToken);
        return error ?? ServiceResult<WarehouseResponse>.Success(ToResponse(item));
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await context.Warehouses.SingleOrDefaultAsync(x => x.WarehouseId == id, cancellationToken);
        if (item is null) return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Warehouse was not found.");
        item.IsActive = false;
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Warehouse was not found."); }
        return ServiceResult<bool>.Success(true);
    }

    private async Task<ServiceResult<WarehouseResponse>?> SaveAsync(CancellationToken cancellationToken)
    {
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        { return ServiceResult<WarehouseResponse>.Failure(ServiceErrorType.Conflict, "Warehouse name already exists."); }
        return null;
    }

    private static void Apply(Warehouse item, WarehouseRequest request)
    {
        item.Name = request.Name.Trim();
        item.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        item.IsActive = request.IsActive;
    }

    private static WarehouseResponse ToResponse(Warehouse item) => new(item.WarehouseId, item.Name, item.Address, item.IsActive);
    private static ServiceResult<WarehouseResponse> NotFound() => ServiceResult<WarehouseResponse>.Failure(ServiceErrorType.NotFound, "Warehouse was not found.");
}
