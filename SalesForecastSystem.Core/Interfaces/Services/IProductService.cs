using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Products;

namespace SalesForecastSystem.Core.Interfaces.Services;

public interface IProductService
{
    Task<PagedResponse<ProductListItemResponse>> GetPagedAsync(
        ProductQueryRequest request,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<ProductResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<ProductStockResponse>> GetStockAsync(int id, CancellationToken cancellationToken = default, int? warehouseId = null);
    Task<ServiceResult<ProductResponse>> CreateAsync(ProductRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<ProductResponse>> UpdateAsync(int id, ProductUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
