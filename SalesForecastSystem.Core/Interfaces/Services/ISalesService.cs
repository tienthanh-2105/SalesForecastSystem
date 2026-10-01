using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Sales;

namespace SalesForecastSystem.Core.Interfaces.Services;

public interface ISalesService
{
    Task<PagedResponse<SalesListItemResponse>> GetPagedAsync(SalesQueryRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<SalesResponse>> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<ServiceResult<SalesResponse>> CreateAsync(SalesRequest request, int userId, CancellationToken cancellationToken);
    Task<ServiceResult<SalesResponse>> UpdateAsync(long id, SalesRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<SalesResponse>> AddItemAsync(long id, SalesItemRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<SalesResponse>> UpdateItemAsync(long id, long itemId, SalesItemRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteItemAsync(long id, long itemId, CancellationToken cancellationToken);
    Task<ServiceResult<SalesResponse>> CompleteAsync(long id, CancellationToken cancellationToken);
    Task<ServiceResult<SalesResponse>> SubmitAsync(long id, CancellationToken cancellationToken);
    Task<ServiceResult<SalesResponse>> DispatchAsync(long id, CancellationToken cancellationToken);
    Task<ServiceResult<SalesResponse>> CancelAsync(long id, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteAsync(long id, CancellationToken cancellationToken);
}
