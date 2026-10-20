namespace SalesForecastSystem.Core.DTOs.Forecasting;

public sealed record ForecastRunResponse(
    long ForecastRunId,
    int ProductId,
    int WarehouseId,
    string Status,
    string ModelName,
    string ModelVersion,
    DateTime TrainingStartDate,
    DateTime TrainingEndDate,
    DateTime ValidationStartDate,
    DateTime ValidationEndDate,
    DateTime ForecastStartDate,
    DateTime ForecastEndDate,
    decimal Mae,
    decimal Rmse,
    int EvaluatedValidationPoints,
    int ExcludedPointCount,
    IReadOnlyList<ForecastCandidateResponse> Candidates,
    IReadOnlyList<ForecastValueResponse> Forecasts);

public sealed record ForecastCandidateResponse(string Name, string Version, decimal Mae, decimal Rmse, int EvaluatedValidationPoints);
public sealed record ForecastValueResponse(DateTime Date, decimal Quantity, decimal LowerBound, decimal UpperBound);
