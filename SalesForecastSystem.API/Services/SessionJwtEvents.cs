using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.API.Services;

public sealed class SessionJwtEvents(AppDbContext context) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext validationContext)
    {
        var principal = validationContext.Principal;
        if (!Guid.TryParse(principal?.FindFirst("jti")?.Value, out var sessionId) ||
            !int.TryParse(principal?.FindFirst("sub")?.Value, out var userId))
        {
            validationContext.Fail("Invalid login session.");
            return;
        }

        var session = await context.LoginSessions
            .AsNoTracking()
            .Include(item => item.User)
            .ThenInclude(user => user.Role)
            .SingleOrDefaultAsync(
                item => item.SessionId == sessionId && item.UserId == userId,
                validationContext.HttpContext.RequestAborted);

        var tokenRole = principal!.FindFirst("role")?.Value;
        if (session is null ||
            session.RevokedAt is not null ||
            session.ExpiresAt <= DateTime.UtcNow ||
            session.User.Status != UserStatuses.Active ||
            !session.User.Role.IsActive ||
            session.User.Role.Name != tokenRole)
        {
            validationContext.Fail("The session expired, was revoked, or the account permissions changed.");
        }
    }
}
