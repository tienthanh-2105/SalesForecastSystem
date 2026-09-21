using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;

namespace SalesForecastSystem.Infrastructure.Services;

public sealed class SessionService(AppDbContext context) : ISessionService
{
    public async Task RevokeAsync(
        Guid sessionId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        await context.LoginSessions
            .Where(session =>
                session.SessionId == sessionId &&
                session.UserId == userId &&
                session.RevokedAt == null)
            .ExecuteUpdateAsync(
                update => update.SetProperty(session => session.RevokedAt, DateTime.UtcNow),
                cancellationToken);
    }
}
