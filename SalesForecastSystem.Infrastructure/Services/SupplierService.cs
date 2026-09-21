using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Suppliers;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Services;

public sealed class SupplierService(AppDbContext context) : ISupplierService
{
    public async Task<PagedResponse<SupplierResponse>> GetPagedAsync(SupplierQueryRequest request, CancellationToken cancellationToken = default)
    {
        var query = context.Suppliers.AsNoTracking().AsQueryable();
        var search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search)) query = query.Where(item => item.Name.Contains(search) ||
            (item.TaxCode != null && item.TaxCode.Contains(search)) ||
            (item.Email != null && item.Email.Contains(search)) ||
            (item.PhoneNumber != null && item.PhoneNumber.Contains(search)));
        if (request.IsActive.HasValue) query = query.Where(item => item.IsActive == request.IsActive.Value);
        var total = await query.CountAsync(cancellationToken);
        var skip = ((long)request.Page - 1) * request.PageSize;
        var items = skip >= total ? [] : await query.OrderBy(item => item.Name).ThenBy(item => item.SupplierId)
            .Skip((int)skip).Take(request.PageSize)
            .Select(item => new SupplierResponse(item.SupplierId, item.Name, item.TaxCode, item.Email, item.PhoneNumber, item.Address, item.IsActive))
            .ToListAsync(cancellationToken);
        return new PagedResponse<SupplierResponse>(items, request.Page, request.PageSize, total,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize));
    }

    public async Task<ServiceResult<SupplierResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await context.Suppliers.AsNoTracking().SingleOrDefaultAsync(x => x.SupplierId == id, cancellationToken);
        return item is null ? NotFound() : ServiceResult<SupplierResponse>.Success(ToResponse(item));
    }

    public async Task<ServiceResult<SupplierResponse>> CreateAsync(SupplierRequest request, CancellationToken cancellationToken = default)
    {
        var item = new Supplier();
        Apply(item, request);
        context.Suppliers.Add(item);
        var error = await SaveAsync(cancellationToken);
        return error ?? ServiceResult<SupplierResponse>.Success(ToResponse(item));
    }

    public async Task<ServiceResult<SupplierResponse>> UpdateAsync(int id, SupplierRequest request, CancellationToken cancellationToken = default)
    {
        var item = await context.Suppliers.SingleOrDefaultAsync(x => x.SupplierId == id, cancellationToken);
        if (item is null) return NotFound();
        Apply(item, request);
        var error = await SaveAsync(cancellationToken);
        return error ?? ServiceResult<SupplierResponse>.Success(ToResponse(item));
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await context.Suppliers.SingleOrDefaultAsync(x => x.SupplierId == id, cancellationToken);
        if (item is null) return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Supplier was not found.");
        item.IsActive = false;
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Supplier was not found."); }
        return ServiceResult<bool>.Success(true);
    }

    private async Task<ServiceResult<SupplierResponse>?> SaveAsync(CancellationToken cancellationToken)
    {
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        { return ServiceResult<SupplierResponse>.Failure(ServiceErrorType.Conflict, "Supplier tax code already exists."); }
        return null;
    }

    private static void Apply(Supplier item, SupplierRequest request)
    {
        item.Name = request.Name.Trim();
        item.TaxCode = Normalize(request.TaxCode);
        item.Email = Normalize(request.Email)?.ToLowerInvariant();
        item.PhoneNumber = Normalize(request.PhoneNumber);
        item.Address = Normalize(request.Address);
        item.IsActive = request.IsActive;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static SupplierResponse ToResponse(Supplier item) => new(item.SupplierId, item.Name, item.TaxCode, item.Email, item.PhoneNumber, item.Address, item.IsActive);
    private static ServiceResult<SupplierResponse> NotFound() => ServiceResult<SupplierResponse>.Failure(ServiceErrorType.NotFound, "Supplier was not found.");
}
