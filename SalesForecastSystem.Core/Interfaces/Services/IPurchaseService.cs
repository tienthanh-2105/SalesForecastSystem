using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Purchases;

namespace SalesForecastSystem.Core.Interfaces.Services;

public interface IPurchaseService
{
    Task<PagedResponse<PurchaseListItemResponse>> GetPagedAsync(PurchaseQueryRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<PurchaseResponse>> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<ServiceResult<PurchaseResponse>> CreateAsync(PurchaseRequest request, int userId, CancellationToken cancellationToken);
    Task<ServiceResult<PurchaseResponse>> UpdateAsync(long id, PurchaseRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<PurchaseResponse>> AddItemAsync(long id, PurchaseItemRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<PurchaseResponse>> UpdateItemAsync(long id, long itemId, PurchaseItemRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteItemAsync(long id, long itemId, CancellationToken cancellationToken);
    Task<ServiceResult<PurchaseResponse>> PostAsync(long id, CancellationToken cancellationToken);
    Task<ServiceResult<PurchaseResponse>> CancelAsync(long id, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteAsync(long id, CancellationToken cancellationToken);
}
