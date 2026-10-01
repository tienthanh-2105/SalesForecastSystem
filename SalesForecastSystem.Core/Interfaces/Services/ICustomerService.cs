using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Customers;

namespace SalesForecastSystem.Core.Interfaces.Services;

public interface ICustomerService
{
    Task<PagedResponse<CustomerResponse>> GetPagedAsync(CustomerQueryRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<CustomerResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<CustomerResponse>> CreateAsync(CustomerRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<CustomerResponse>> UpdateAsync(int id, CustomerRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> DeactivateAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResponse<CustomerOrderResponse>>> GetOrdersAsync(int id, int page, int pageSize, CancellationToken cancellationToken = default);
}
