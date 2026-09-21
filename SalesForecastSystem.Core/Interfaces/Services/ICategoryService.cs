using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Categories;

namespace SalesForecastSystem.Core.Interfaces.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<CategoryResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<CategoryResponse>> CreateAsync(CategoryRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<CategoryResponse>> UpdateAsync(int id, CategoryRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
