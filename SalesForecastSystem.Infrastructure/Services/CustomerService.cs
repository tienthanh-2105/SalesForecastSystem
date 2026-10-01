using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Customers;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Services;

public sealed class CustomerService(AppDbContext context) : ICustomerService
{
    public async Task<PagedResponse<CustomerResponse>> GetPagedAsync(CustomerQueryRequest request, CancellationToken cancellationToken = default)
    {
        var query = context.Customers.AsNoTracking().AsQueryable();
        var search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search)) query = query.Where(item => item.FullName.Contains(search) ||
            (item.Email != null && item.Email.Contains(search)) ||
            (item.PhoneNumber != null && item.PhoneNumber.Contains(search)));
        if (request.IsActive.HasValue) query = query.Where(item => item.IsActive == request.IsActive.Value);
        var total = await query.CountAsync(cancellationToken);
        var skip = ((long)request.Page - 1) * request.PageSize;
        var items = skip >= total ? [] : await query.OrderBy(item => item.FullName).ThenBy(item => item.CustomerId)
            .Skip((int)skip).Take(request.PageSize)
            .Select(item => new CustomerResponse(item.CustomerId, item.FullName, item.Email,
                item.PhoneNumber, item.Address, item.CreatedAt, item.IsActive))
            .ToListAsync(cancellationToken);
        return Page(items, request.Page, request.PageSize, total);
    }

    public async Task<ServiceResult<CustomerResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await context.Customers.AsNoTracking().SingleOrDefaultAsync(x => x.CustomerId == id, cancellationToken);
        return item is null ? NotFound() : ServiceResult<CustomerResponse>.Success(ToResponse(item));
    }

    public async Task<ServiceResult<CustomerResponse>> CreateAsync(CustomerRequest request, CancellationToken cancellationToken = default)
    {
        var item = new Customer();
        Apply(item, request);
        context.Customers.Add(item);
        var error = await SaveAsync(cancellationToken);
        return error ?? ServiceResult<CustomerResponse>.Success(ToResponse(item));
    }

    public async Task<ServiceResult<CustomerResponse>> UpdateAsync(int id, CustomerRequest request, CancellationToken cancellationToken = default)
    {
        var item = await context.Customers.SingleOrDefaultAsync(x => x.CustomerId == id, cancellationToken);
        if (item is null) return NotFound();
        Apply(item, request);
        var error = await SaveAsync(cancellationToken);
        return error ?? ServiceResult<CustomerResponse>.Success(ToResponse(item));
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await context.Customers.SingleOrDefaultAsync(x => x.CustomerId == id, cancellationToken);
        if (item is null) return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Customer was not found.");
        item.IsActive = false;
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Customer was not found."); }
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<PagedResponse<CustomerOrderResponse>>> GetOrdersAsync(int id, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (!await context.Customers.AnyAsync(x => x.CustomerId == id, cancellationToken))
            return ServiceResult<PagedResponse<CustomerOrderResponse>>.Failure(ServiceErrorType.NotFound, "Customer was not found.");
        var query = context.SalesOrders.AsNoTracking().Where(x => x.CustomerId == id);
        var total = await query.CountAsync(cancellationToken);
        var skip = ((long)page - 1) * pageSize;
        var items = skip >= total ? [] : await query.OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.SalesOrderId)
            .Skip((int)skip).Take(pageSize)
            .Select(x => new CustomerOrderResponse(x.SalesOrderId, x.OrderNumber, x.OrderDate, x.Status, x.CreatedAt))
            .ToListAsync(cancellationToken);
        return ServiceResult<PagedResponse<CustomerOrderResponse>>.Success(Page(items, page, pageSize, total));
    }

    private async Task<ServiceResult<CustomerResponse>?> SaveAsync(CancellationToken cancellationToken)
    {
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        { return ServiceResult<CustomerResponse>.Failure(ServiceErrorType.Conflict, "Customer email already exists."); }
        return null;
    }

    private static void Apply(Customer item, CustomerRequest request)
    {
        item.FullName = request.FullName.Trim();
        item.Email = Normalize(request.Email)?.ToLowerInvariant();
        item.PhoneNumber = Normalize(request.PhoneNumber);
        item.Address = Normalize(request.Address);
        item.IsActive = request.IsActive;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static CustomerResponse ToResponse(Customer item) => new(item.CustomerId, item.FullName,
        item.Email, item.PhoneNumber, item.Address, item.CreatedAt, item.IsActive);
    private static ServiceResult<CustomerResponse> NotFound() =>
        ServiceResult<CustomerResponse>.Failure(ServiceErrorType.NotFound, "Customer was not found.");
    private static PagedResponse<T> Page<T>(IReadOnlyList<T> items, int page, int pageSize, int total) =>
        new(items, page, pageSize, total, total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize));
}
