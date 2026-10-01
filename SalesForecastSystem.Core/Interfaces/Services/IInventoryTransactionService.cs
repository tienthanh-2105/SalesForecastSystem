using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Inventory;

namespace SalesForecastSystem.Core.Interfaces.Services;

public interface IInventoryTransactionService
{
    Task<PagedResponse<InventoryTransactionResponse>> GetPagedAsync(InventoryTransactionQuery request, CancellationToken cancellationToken);
}
