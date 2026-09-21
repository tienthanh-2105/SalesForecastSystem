using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Suppliers;

namespace SalesForecastSystem.Core.Interfaces.Services;

public interface ISupplierService
{
    Task<PagedResponse<SupplierResponse>> GetPagedAsync(SupplierQueryRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<SupplierResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<SupplierResponse>> CreateAsync(SupplierRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<SupplierResponse>> UpdateAsync(int id, SupplierRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
