using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
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
        var expiresAt = now.AddMinutes(15);
        var sessionId = Guid.NewGuid();

        var claims = new[]
        {
            new Claim("sub", user.UserId.ToString(CultureInfo.InvariantCulture)),
            new Claim("jti", sessionId.ToString()),
            new Claim("email", user.Email),
            new Claim("name", user.FullName),
            new Claim("role", user.Role)
        };

        var signingKey = GetRequiredSetting("Jwt:Key");
        var issuer = GetRequiredSetting("Jwt:Issuer");
        var audience = GetRequiredSetting("Jwt:Audience");
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            now,
            expiresAt,
            credentials);

        context.LoginSessions.Add(new LoginSession
        {
            SessionId = sessionId,
            UserId = user.UserId,
            CreatedAt = now,
            ExpiresAt = expiresAt
        });
        await context.SaveChangesAsync(cancellationToken);

        return new LoginResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            TokenType = "Bearer",
            ExpiresAt = expiresAt,
            User = user
        };
    }

    private string GetRequiredSetting(string key) =>
        configuration[key] ?? throw new InvalidOperationException($"Missing configuration value: {key}.");
}
