using System.Text;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.DTOs.Auth;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Services;

public sealed class AuthService(
    AppDbContext context,
    ITokenService tokenService) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrEmpty(request.Password) ||
            Encoding.UTF8.GetByteCount(request.Password) > 72)
        {
            return null;
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await context.Users
            .AsNoTracking()
            .Include(account => account.Role)
            .SingleOrDefaultAsync(account => account.Email == normalizedEmail, cancellationToken);

        if (user is null ||
            user.Status != UserStatuses.Active ||
            !user.Role.IsActive)
        {
            return null;
        }

        bool passwordMatches;
        try
        {
            passwordMatches = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return null;
        }

        if (!passwordMatches)
        {
            return null;
        }

        var userResponse = new LoginUserResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.Name
        };

        return await tokenService.CreateAccessTokenAsync(userResponse, cancellationToken);
    }
}
