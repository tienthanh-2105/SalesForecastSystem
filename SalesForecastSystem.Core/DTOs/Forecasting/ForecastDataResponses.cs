namespace SalesForecastSystem.Core.DTOs.Forecasting;

public static class ForecastDataStatuses
{
    public const string Sold = "Sold";
    public const string NoSale = "NoSale";
    public const string StockOut = "StockOut";
    public const string MissingInventoryHistory = "MissingInventoryHistory";
}

public static class ForecastDatasetSplits
{
    public const string Training = "Training";
    public const string Validation = "Validation";
}

public sealed record ForecastDailyPointResponse(
    DateTime Date,
    long QuantitySold,
    long ClosingStock,
    string DataStatus,
    string DatasetSplit);

public sealed record ForecastDatasetResponse(
    int ProductId,
    string SKU,
    string ProductName,
    int? WarehouseId,
    DateTime FromDate,
    DateTime ToDate,
    int ValidationPercentage,
    DateTime ValidationStartDate,
    int TrainingPointCount,
    int ValidationPointCount,
    long TotalQuantitySold,
    IReadOnlyList<ForecastDailyPointResponse> Points);
