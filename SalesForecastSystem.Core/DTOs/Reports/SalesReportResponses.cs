namespace SalesForecastSystem.Core.DTOs.Reports;

public sealed record SalesSummaryResponse(
    DateTime? FromDate,
    DateTime? ToDate,
    long OrderCount,
    long QuantitySold,
    decimal Revenue);

public sealed record SalesTrendPointResponse(
    DateTime PeriodStart,
    long OrderCount,
    long QuantitySold,
    decimal Revenue);

public sealed record ProductSalesResponse(
    int ProductId,
    string SKU,
    string ProductName,
    long OrderCount,
    long QuantitySold,
    decimal Revenue);

public sealed record CategorySalesResponse(
    int CategoryId,
    string CategoryName,
    long OrderCount,
    long QuantitySold,
    decimal Revenue);

public sealed record StaffSalesResponse(
    int UserId,
    string FullName,
    long OrderCount,
    long QuantitySold,
    decimal Revenue);

public sealed record InventoryReportResponse(
    int ProductId,
    string SKU,
    string ProductName,
    int MinimumStockLevel,
    long QuantityOnHand,
    bool IsBelowMinimum);
