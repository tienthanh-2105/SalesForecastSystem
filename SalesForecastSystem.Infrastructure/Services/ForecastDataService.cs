using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Forecasting;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;

namespace SalesForecastSystem.Infrastructure.Services;

public sealed class ForecastDataService(AppDbContext context) : IForecastDataService
{
    public async Task<ServiceResult<ForecastDatasetResponse>> GetDailyDatasetAsync(
        ForecastDataQuery request,
        CancellationToken cancellationToken = default)
    {
        var productId = request.ProductId!.Value;
        var product = await context.Products.AsNoTracking()
            .Where(row => row.ProductId == productId)
            .Select(row => new { row.ProductId, row.SKU, row.Name })
            .SingleOrDefaultAsync(cancellationToken);
        if (product is null)
        {
            return ServiceResult<ForecastDatasetResponse>.Failure(
                ServiceErrorType.NotFound, "Product was not found.");
        }

        if (request.WarehouseId.HasValue &&
            !await context.Warehouses.AsNoTracking()
                .AnyAsync(row => row.WarehouseId == request.WarehouseId.Value, cancellationToken))
        {
            return ServiceResult<ForecastDatasetResponse>.Failure(
                ServiceErrorType.NotFound, "Warehouse was not found.");
        }

        var fromDate = request.FromDate!.Value.Date;
        var toDate = request.ToDate!.Value.Date;
        var salesQuery =
            from order in context.SalesOrders.AsNoTracking()
            join item in context.SalesOrderItems.AsNoTracking()
                on order.SalesOrderId equals item.SalesOrderId
            where order.Status == "Completed" &&
                  item.ProductId == productId &&
                  order.OrderDate >= fromDate &&
                  order.OrderDate <= toDate &&
                  (!request.WarehouseId.HasValue || order.WarehouseId == request.WarehouseId.Value)
            group item by order.OrderDate into daily
            select new
            {
                Date = daily.Key,
                QuantitySold = daily.Sum(row => (long)row.Quantity)
            };

        var salesByDate = (await salesQuery.ToListAsync(cancellationToken))
            .ToDictionary(row => row.Date.Date, row => row.QuantitySold);

        var inventoryQuery = context.InventoryTransactions.AsNoTracking()
            .Where(row => row.ProductId == productId && row.TransactionDate <= toDate);
        if (request.WarehouseId.HasValue)
            inventoryQuery = inventoryQuery.Where(row => row.WarehouseId == request.WarehouseId.Value);

        var inventoryRows = await inventoryQuery
            .Select(row => new { row.TransactionDate, row.Quantity })
            .OrderBy(row => row.TransactionDate)
            .ToListAsync(cancellationToken);
        var inventoryByDate = inventoryRows
            .GroupBy(row => row.TransactionDate.Date)
            .ToDictionary(group => group.Key, group => group.Sum(row => (long)row.Quantity));

        var pointCount = (toDate - fromDate).Days + 1;
        var validationPointCount = Math.Clamp(
            (int)Math.Ceiling(pointCount * request.ValidationPercentage / 100d), 1, pointCount - 1);
        var trainingPointCount = pointCount - validationPointCount;
        var validationStartDate = fromDate.AddDays(trainingPointCount);

        long closingStock = inventoryRows
            .Where(row => row.TransactionDate.Date < fromDate)
            .Sum(row => (long)row.Quantity);
        var hasInventoryHistory = inventoryRows.Any(row => row.TransactionDate.Date < fromDate);
        var points = new List<ForecastDailyPointResponse>(pointCount);
        long totalQuantitySold = 0;

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            if (inventoryByDate.TryGetValue(date, out var inventoryChange))
            {
                closingStock += inventoryChange;
                hasInventoryHistory = true;
            }

            var quantitySold = salesByDate.GetValueOrDefault(date);
            totalQuantitySold += quantitySold;
            var dataStatus = quantitySold > 0
                ? ForecastDataStatuses.Sold
                : !hasInventoryHistory
                    ? ForecastDataStatuses.MissingInventoryHistory
                    : closingStock <= 0
                        ? ForecastDataStatuses.StockOut
                        : ForecastDataStatuses.NoSale;
            var split = date < validationStartDate
                ? ForecastDatasetSplits.Training
                : ForecastDatasetSplits.Validation;

            points.Add(new ForecastDailyPointResponse(
                date, quantitySold, closingStock, dataStatus, split));
        }

        return ServiceResult<ForecastDatasetResponse>.Success(new ForecastDatasetResponse(
            product.ProductId,
            product.SKU,
            product.Name,
            request.WarehouseId,
            fromDate,
            toDate,
            request.ValidationPercentage,
            validationStartDate,
            trainingPointCount,
            validationPointCount,
            totalQuantitySold,
            points));
    }
}
