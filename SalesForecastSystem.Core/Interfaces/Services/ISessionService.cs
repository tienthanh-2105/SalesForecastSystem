namespace SalesForecastSystem.Core.Interfaces.Services;

public interface ISessionService
{
    Task RevokeAsync(Guid sessionId, int userId, CancellationToken cancellationToken = default);
}
