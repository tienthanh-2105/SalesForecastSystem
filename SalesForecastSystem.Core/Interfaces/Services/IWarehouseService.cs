using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Warehouses;

namespace SalesForecastSystem.Core.Interfaces.Services;

public interface IWarehouseService
{
    Task<PagedResponse<WarehouseResponse>> GetPagedAsync(WarehouseQueryRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<WarehouseResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<WarehouseResponse>> CreateAsync(WarehouseRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<WarehouseResponse>> UpdateAsync(int id, WarehouseRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
