using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.DTOs.Reports;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;

namespace SalesForecastSystem.Infrastructure.Services;

public sealed class ReportService(AppDbContext context) : IReportService
{
    public async Task<SalesSummaryResponse> GetSalesSummaryAsync(
        SalesReportQuery request,
        CancellationToken cancellationToken = default)
    {
        var lines = BuildSalesLines(request);
        var totals = await lines
            .GroupBy(_ => 1)
            .Select(group => new
            {
                OrderCount = group.Select(row => row.SalesOrderId).Distinct().LongCount(),
                QuantitySold = group.Sum(row => (long)row.Quantity),
                Revenue = group.Sum(row => row.LineTotal)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return new SalesSummaryResponse(
            request.FromDate?.Date,
            request.ToDate?.Date,
            totals?.OrderCount ?? 0,
            totals?.QuantitySold ?? 0,
            totals?.Revenue ?? 0);
    }

    public async Task<IReadOnlyList<SalesTrendPointResponse>> GetSalesTrendAsync(
        SalesTrendQuery request,
        CancellationToken cancellationToken = default)
    {
        var dailyRows = await BuildSalesLines(request)
            .GroupBy(row => row.OrderDate)
            .Select(group => new
            {
                Date = group.Key,
                OrderCount = group.Select(row => row.SalesOrderId).Distinct().LongCount(),
                QuantitySold = group.Sum(row => (long)row.Quantity),
                Revenue = group.Sum(row => row.LineTotal)
            })
            .OrderBy(row => row.Date)
            .ToListAsync(cancellationToken);

        return dailyRows
            .GroupBy(row => GetPeriodStart(row.Date, request.Period))
            .OrderBy(group => group.Key)
            .Select(group => new SalesTrendPointResponse(
                group.Key,
                group.Sum(row => row.OrderCount),
                group.Sum(row => row.QuantitySold),
                group.Sum(row => row.Revenue)))
            .ToList();
    }

    public async Task<IReadOnlyList<ProductSalesResponse>> GetTopProductsAsync(
        SalesRankingQuery request,
        CancellationToken cancellationToken = default)
    {
        var rows = await BuildSalesLines(request)
            .GroupBy(row => new { row.ProductId, row.SKU, row.ProductName })
            .Select(group => new
            {
                group.Key.ProductId,
                group.Key.SKU,
                group.Key.ProductName,
                OrderCount = group.Select(row => row.SalesOrderId).Distinct().LongCount(),
                QuantitySold = group.Sum(row => (long)row.Quantity),
                Revenue = group.Sum(row => row.LineTotal)
            })
            .OrderByDescending(row => row.Revenue)
            .ThenBy(row => row.ProductId)
            .Take(request.Top)
            .ToListAsync(cancellationToken);

        return rows.Select(row => new ProductSalesResponse(
            row.ProductId, row.SKU, row.ProductName, row.OrderCount, row.QuantitySold, row.Revenue)).ToList();
    }

    public async Task<IReadOnlyList<CategorySalesResponse>> GetTopCategoriesAsync(
        SalesRankingQuery request,
        CancellationToken cancellationToken = default)
    {
        var rows = await BuildSalesLines(request)
            .GroupBy(row => new { row.CategoryId, row.CategoryName })
            .Select(group => new
            {
                group.Key.CategoryId,
                group.Key.CategoryName,
                OrderCount = group.Select(row => row.SalesOrderId).Distinct().LongCount(),
                QuantitySold = group.Sum(row => (long)row.Quantity),
                Revenue = group.Sum(row => row.LineTotal)
            })
            .OrderByDescending(row => row.Revenue)
            .ThenBy(row => row.CategoryId)
            .Take(request.Top)
            .ToListAsync(cancellationToken);

        return rows.Select(row => new CategorySalesResponse(
            row.CategoryId, row.CategoryName, row.OrderCount, row.QuantitySold, row.Revenue)).ToList();
    }

    public async Task<IReadOnlyList<StaffSalesResponse>> GetSalesByStaffAsync(
        SalesRankingQuery request,
        CancellationToken cancellationToken = default)
    {
        var rows = await BuildSalesLines(request)
            .GroupBy(row => new { row.StaffUserId, row.StaffName })
            .Select(group => new
            {
                UserId = group.Key.StaffUserId,
                FullName = group.Key.StaffName,
                OrderCount = group.Select(row => row.SalesOrderId).Distinct().LongCount(),
                QuantitySold = group.Sum(row => (long)row.Quantity),
                Revenue = group.Sum(row => row.LineTotal)
            })
            .OrderByDescending(row => row.Revenue)
            .ThenBy(row => row.UserId)
            .Take(request.Top)
            .ToListAsync(cancellationToken);

        return rows.Select(row => new StaffSalesResponse(
            row.UserId, row.FullName, row.OrderCount, row.QuantitySold, row.Revenue)).ToList();
    }

    public async Task<IReadOnlyList<InventoryReportResponse>> GetInventoryAsync(
        InventoryReportQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = context.Products.AsNoTracking().Select(product => new
        {
            product.ProductId,
            product.SKU,
            ProductName = product.Name,
            product.MinimumStockLevel,
            QuantityOnHand = context.InventoryBalances
                .Where(balance => balance.ProductId == product.ProductId &&
                    (!request.WarehouseId.HasValue || balance.WarehouseId == request.WarehouseId.Value))
                .Sum(balance => (long?)balance.QuantityOnHand) ?? 0
        });

        if (request.BelowMinimumOnly)
        {
            query = query.Where(row => row.QuantityOnHand <= row.MinimumStockLevel);
        }

        var rows = await query
            .OrderBy(row => row.QuantityOnHand)
            .ThenBy(row => row.ProductName)
            .ThenBy(row => row.ProductId)
            .ToListAsync(cancellationToken);

        return rows.Select(row => new InventoryReportResponse(
            row.ProductId,
            row.SKU,
            row.ProductName,
            row.MinimumStockLevel,
            row.QuantityOnHand,
            row.QuantityOnHand <= row.MinimumStockLevel)).ToList();
    }

    private IQueryable<SalesLineRow> BuildSalesLines(SalesReportQuery request)
    {
        var query =
            from order in context.SalesOrders.AsNoTracking()
            join item in context.SalesOrderItems.AsNoTracking()
                on order.SalesOrderId equals item.SalesOrderId
            join product in context.Products.AsNoTracking()
                on item.ProductId equals product.ProductId
            join category in context.Categories.AsNoTracking()
                on product.CategoryId equals category.CategoryId
            join user in context.Users.AsNoTracking()
                on order.CreatedByUserId equals user.UserId
            where order.Status == "Completed"
            select new SalesLineRow
            {
                SalesOrderId = order.SalesOrderId,
                OrderDate = order.OrderDate,
                WarehouseId = order.WarehouseId,
                StaffUserId = order.CreatedByUserId,
                StaffName = user.FullName,
                ProductId = product.ProductId,
                SKU = product.SKU,
                ProductName = product.Name,
                CategoryId = category.CategoryId,
                CategoryName = category.Name,
                Quantity = item.Quantity,
                LineTotal = item.LineTotal
            };

        if (request.FromDate.HasValue)
            query = query.Where(row => row.OrderDate >= request.FromDate.Value.Date);
        if (request.ToDate.HasValue)
            query = query.Where(row => row.OrderDate <= request.ToDate.Value.Date);
        if (request.WarehouseId.HasValue)
            query = query.Where(row => row.WarehouseId == request.WarehouseId.Value);
        if (request.ProductId.HasValue)
            query = query.Where(row => row.ProductId == request.ProductId.Value);
        if (request.CategoryId.HasValue)
            query = query.Where(row => row.CategoryId == request.CategoryId.Value);
        if (request.StaffUserId.HasValue)
            query = query.Where(row => row.StaffUserId == request.StaffUserId.Value);

        return query;
    }

    private static DateTime GetPeriodStart(DateTime date, SalesReportPeriod period) => period switch
    {
        SalesReportPeriod.Month => new DateTime(date.Year, date.Month, 1),
        SalesReportPeriod.Week => date.Date.AddDays(-((7 + (int)date.DayOfWeek - (int)DayOfWeek.Monday) % 7)),
        _ => date.Date
    };

    private sealed class SalesLineRow
    {
        public long SalesOrderId { get; init; }
        public DateTime OrderDate { get; init; }
        public int WarehouseId { get; init; }
        public int StaffUserId { get; init; }
        public string StaffName { get; init; } = string.Empty;
        public int ProductId { get; init; }
        public string SKU { get; init; } = string.Empty;
        public string ProductName { get; init; } = string.Empty;
        public int CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal LineTotal { get; init; }
    }

}
