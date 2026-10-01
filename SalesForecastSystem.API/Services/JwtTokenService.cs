using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.DTOs.Auth;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.API.Services;

public sealed class JwtTokenService(
    IConfiguration configuration,
    AppDbContext context) : ITokenService
{
    public async Task<LoginResponse> CreateAccessTokenAsync(
        LoginUserResponse user,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(GetTokenLifetimeMinutes());
        var sessionId = Guid.NewGuid();

        context.LoginSessions.Add(new LoginSession
        {
            SessionId = sessionId,
            UserId = user.UserId,
            CreatedAt = now,
            ExpiresAt = expiresAt
        });
        await context.SaveChangesAsync(cancellationToken);

        return CreateResponse(user, sessionId, now, expiresAt);
    }

    public async Task<LoginResponse?> RefreshAccessTokenAsync(
        Guid sessionId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var session = await context.LoginSessions
            .Include(item => item.User)
            .ThenInclude(user => user.Role)
            .SingleOrDefaultAsync(
                item => item.SessionId == sessionId && item.UserId == userId,
                cancellationToken);

        var now = DateTime.UtcNow;
        if (session is null ||
            session.RevokedAt is not null ||
            session.ExpiresAt <= now ||
            session.User.Status != UserStatuses.Active ||
            !session.User.Role.IsActive)
        {
            return null;
        }

        var expiresAt = now.AddMinutes(GetTokenLifetimeMinutes());
        session.ExpiresAt = expiresAt;
        await context.SaveChangesAsync(cancellationToken);

        var user = new LoginUserResponse
        {
            UserId = session.User.UserId,
            FullName = session.User.FullName,
            Email = session.User.Email,
            Role = session.User.Role.Name
        };

        return CreateResponse(user, sessionId, now, expiresAt);
    }

    private LoginResponse CreateResponse(
        LoginUserResponse user,
        Guid sessionId,
        DateTime issuedAt,
        DateTime expiresAt)
    {
        var claims = new[]
        {
            new Claim("sub", user.UserId.ToString(CultureInfo.InvariantCulture)),
            new Claim("jti", sessionId.ToString()),
            new Claim("email", user.Email),
            new Claim("name", user.FullName),
            new Claim("role", user.Role)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetRequiredSetting("Jwt:Key"))),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            GetRequiredSetting("Jwt:Issuer"),
            GetRequiredSetting("Jwt:Audience"),
            claims,
            issuedAt,
            expiresAt,
            credentials);

        return new LoginResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            TokenType = "Bearer",
            ExpiresAt = expiresAt,
            User = user
        };
    }

    private int GetTokenLifetimeMinutes() =>
        int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var minutes)
            ? Math.Clamp(minutes, 16, 480)
            : 30;

    private string GetRequiredSetting(string key) =>
        configuration[key] ?? throw new InvalidOperationException($"Missing configuration value: {key}.");
}
