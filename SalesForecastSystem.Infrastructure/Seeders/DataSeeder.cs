using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.Helpers;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Seeders;

public static class DataSeeder
{
    public static async Task SeedAdminAsync(
        AppDbContext context,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (await context.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken))
        {
            return;
        }

        ValidateAdminPassword(password);
        if (normalizedEmail.Length > 100 || !Regex.IsMatch(normalizedEmail, AccountValidation.EmailPattern))
            throw new InvalidOperationException("Admin email is invalid.");

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var adminRole = await context.Roles
            .SingleOrDefaultAsync(role => role.Name == RoleNames.Admin, cancellationToken);

        if (adminRole is null)
        {
            adminRole = new Role
            {
                Name = RoleNames.Admin,
                Description = "System administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Roles.Add(adminRole);
        }

        context.Users.Add(new User
        {
            Role = adminRole,
            FullName = "Administrator",
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
            Status = UserStatuses.Active,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static void ValidateAdminPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            password == "THAY_BANG_MAT_KHAU_RIENG" ||
            password.Length < 12 ||
            !Regex.IsMatch(password, AccountValidation.PasswordPattern) ||
            Encoding.UTF8.GetByteCount(password) > 72)
        {
            throw new InvalidOperationException(
                "Admin password must contain at least 12 characters, a letter and a digit, and no more than 72 UTF-8 bytes.");
        }
    }
}
