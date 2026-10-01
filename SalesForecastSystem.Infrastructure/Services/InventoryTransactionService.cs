using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Inventory;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;

namespace SalesForecastSystem.Infrastructure.Services;

public sealed class InventoryTransactionService(AppDbContext context) : IInventoryTransactionService
{
    public async Task<PagedResponse<InventoryTransactionResponse>> GetPagedAsync(
        InventoryTransactionQuery request, CancellationToken cancellationToken)
    {
        var query = context.InventoryTransactions.AsNoTracking().AsQueryable();
        if (request.WarehouseId.HasValue) query = query.Where(x => x.WarehouseId == request.WarehouseId);
        if (request.ProductId.HasValue) query = query.Where(x => x.ProductId == request.ProductId);
        if (request.FromDate.HasValue) query = query.Where(x => x.TransactionDate >= request.FromDate.Value.Date);
        if (request.ToDate.HasValue) query = query.Where(x => x.TransactionDate <= request.ToDate.Value.Date);
        var total = await query.CountAsync(cancellationToken);
        var skip = ((long)request.Page - 1) * request.PageSize;
        var items = skip >= total ? [] : await query.OrderByDescending(x => x.TransactionDate)
            .ThenByDescending(x => x.InventoryTransactionId).Skip((int)skip).Take(request.PageSize)
            .Select(x => new InventoryTransactionResponse(x.InventoryTransactionId, x.WarehouseId,
                x.ProductId, x.PurchaseOrderItemId, x.SalesOrderItemId, x.Quantity,
                x.TransactionDate, x.CreatedAt)).ToListAsync(cancellationToken);
        return new PagedResponse<InventoryTransactionResponse>(items, request.Page, request.PageSize,
            total, total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize));
    }
}
