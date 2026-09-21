namespace SalesForecastSystem.Core.Common;

public enum ServiceErrorType
{
    NotFound,
    Conflict,
    Validation
}

public sealed record ServiceError(
    ServiceErrorType Type,
    string Message,
    string? FieldName = null);
