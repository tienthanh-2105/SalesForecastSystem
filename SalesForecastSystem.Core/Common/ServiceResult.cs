namespace SalesForecastSystem.Core.Common;

public sealed class ServiceResult<T>
{
    private ServiceResult(T value)
    {
        Value = value;
    }

    private ServiceResult(ServiceError error)
    {
        Error = error;
    }

    public T? Value { get; }
    public ServiceError? Error { get; }
    public bool IsSuccess => Error is null;

    public static ServiceResult<T> Success(T value) => new(value);

    public static ServiceResult<T> Failure(
        ServiceErrorType type,
        string message,
        string? fieldName = null) => new(new ServiceError(type, message, fieldName));
}
