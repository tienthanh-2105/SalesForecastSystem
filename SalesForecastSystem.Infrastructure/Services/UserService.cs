using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Users;
using SalesForecastSystem.Core.Helpers;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Services;

public sealed class UserService(AppDbContext context) : IUserService
{
    public async Task<PagedResponse<UserResponse>> GetPagedAsync(
        UserQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = context.Users.AsNoTracking().AsQueryable();
        var search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(user =>
                user.FullName.Contains(search) || user.Email.Contains(search));
        }

        var role = NormalizeKnownValue(request.Role, RoleNames.Values);
        if (role is not null)
        {
            query = query.Where(user => user.Role.Name == role);
        }

        var status = NormalizeKnownValue(request.Status, UserStatusValues.Values);
        if (status is not null)
        {
            query = query.Where(user => user.Status == status);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var skip = ((long)request.Page - 1) * request.PageSize;
        var items = skip >= totalItems
            ? []
            : await query
                .OrderBy(user => user.FullName)
                .ThenBy(user => user.UserId)
                .Skip((int)skip)
                .Take(request.PageSize)
                .Select(user => new UserResponse(
                    user.UserId,
                    user.FullName,
                    user.Email,
                    user.PhoneNumber,
                    user.RoleId,
                    user.Role.Name,
                    user.Status,
                    user.CreatedAt,
                    user.UpdatedAt))
                .ToListAsync(cancellationToken);

        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)request.PageSize);
        return new PagedResponse<UserResponse>(
            items,
            request.Page,
            request.PageSize,
            totalItems,
            totalPages);
    }

    public async Task<ServiceResult<UserResponse>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .AsNoTracking()
            .Include(item => item.Role)
            .SingleOrDefaultAsync(item => item.UserId == id, cancellationToken);
        return user is null
            ? NotFound()
            : ServiceResult<UserResponse>.Success(ToResponse(user));
    }

    public async Task<ServiceResult<UserResponse>> CreateAsync(
        UserCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = await FindActiveRoleAsync(request.Role, cancellationToken);
        if (role is null)
        {
            return InvalidRole();
        }

        var email = NormalizeEmail(request.Email);
        if (await EmailExistsAsync(email, null, cancellationToken))
        {
            return DuplicateEmail();
        }

        var user = new User
        {
            RoleId = role.RoleId,
            Role = role,
            FullName = request.FullName.Trim(),
            Email = email,
            PhoneNumber = NormalizeOptional(request.PhoneNumber),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Status = UserStatuses.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        var saveError = await SaveChangesAsync(cancellationToken);
        return saveError ?? ServiceResult<UserResponse>.Success(ToResponse(user));
    }

    public async Task<ServiceResult<UserResponse>> UpdateAsync(
        int id,
        int currentUserId,
        UserUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .Include(item => item.Role)
            .SingleOrDefaultAsync(item => item.UserId == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var role = await FindActiveRoleAsync(request.Role, cancellationToken);
        if (role is null)
        {
            return InvalidRole();
        }

        if (id == currentUserId && user.RoleId != role.RoleId)
        {
            return ServiceResult<UserResponse>.Failure(
                ServiceErrorType.Validation,
                "You cannot change your own role.",
                nameof(request.Role));
        }

        var email = NormalizeEmail(request.Email);
        if (await EmailExistsAsync(email, id, cancellationToken))
        {
            return DuplicateEmail();
        }

        var invalidatesSessions = user.RoleId != role.RoleId ||
                                  user.Email != email ||
                                  user.FullName != request.FullName.Trim();
        user.RoleId = role.RoleId;
        user.Role = role;
        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.PhoneNumber = NormalizeOptional(request.PhoneNumber);
        user.UpdatedAt = DateTime.UtcNow;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var saveError = await SaveChangesAsync(cancellationToken);
        if (saveError is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return saveError;
        }

        if (invalidatesSessions)
        {
            await RevokeSessionsAsync(id, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return ServiceResult<UserResponse>.Success(ToResponse(user));
    }

    public async Task<ServiceResult<UserResponse>> SetStatusAsync(
        int id,
        int currentUserId,
        UserStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .Include(item => item.Role)
            .SingleOrDefaultAsync(item => item.UserId == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var status = NormalizeKnownValue(request.Status, UserStatusValues.Values)!;
        if (id == currentUserId && status == UserStatuses.Locked)
        {
            return ServiceResult<UserResponse>.Failure(
                ServiceErrorType.Validation,
                "You cannot lock your own account.",
                nameof(request.Status));
        }

        if (user.Status == status)
        {
            return ServiceResult<UserResponse>.Success(ToResponse(user));
        }

        user.Status = status;
        user.UpdatedAt = DateTime.UtcNow;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var saveError = await SaveChangesAsync(cancellationToken);
        if (saveError is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return saveError;
        }

        if (status == UserStatuses.Locked)
        {
            await RevokeSessionsAsync(id, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return ServiceResult<UserResponse>.Success(ToResponse(user));
    }

    public async Task<ServiceResult<bool>> ResetPasswordAsync(
        int id,
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users.SingleOrDefaultAsync(
            item => item.UserId == id,
            cancellationToken);
        if (user is null)
        {
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "User was not found.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await RevokeSessionsAsync(id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return ServiceResult<bool>.Success(true);
    }

    private async Task<Role?> FindActiveRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var normalizedRole = NormalizeKnownValue(roleName, RoleNames.Values);
        return normalizedRole is null
            ? null
            : await context.Roles.SingleOrDefaultAsync(
                role => role.Name == normalizedRole && role.IsActive,
                cancellationToken);
    }

    private Task<bool> EmailExistsAsync(string email, int? excludedUserId, CancellationToken cancellationToken) =>
        context.Users.AnyAsync(
            user => user.Email == email &&
                    (!excludedUserId.HasValue || user.UserId != excludedUserId.Value),
            cancellationToken);

    private Task<int> RevokeSessionsAsync(int userId, CancellationToken cancellationToken) =>
        context.LoginSessions
            .Where(session => session.UserId == userId && session.RevokedAt == null)
            .ExecuteUpdateAsync(
                update => update.SetProperty(session => session.RevokedAt, DateTime.UtcNow),
                cancellationToken);

    private async Task<ServiceResult<UserResponse>?> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return DuplicateEmail();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 547 })
        {
            return ServiceResult<UserResponse>.Failure(
                ServiceErrorType.Conflict,
                "Related data changed or violated a database constraint. Reload and try again.");
        }

        return null;
    }

    private static UserResponse ToResponse(User user) => new(
        user.UserId,
        user.FullName,
        user.Email,
        user.PhoneNumber,
        user.RoleId,
        user.Role.Name,
        user.Status,
        user.CreatedAt,
        user.UpdatedAt);

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeKnownValue(string? value, IEnumerable<string> allowedValues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return allowedValues.FirstOrDefault(item =>
            string.Equals(item, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    private static ServiceResult<UserResponse> NotFound() =>
        ServiceResult<UserResponse>.Failure(ServiceErrorType.NotFound, "User was not found.");

    private static ServiceResult<UserResponse> InvalidRole() =>
        ServiceResult<UserResponse>.Failure(
            ServiceErrorType.Validation,
            "Role does not exist or is inactive.",
            nameof(UserWriteRequest.Role));

    private static ServiceResult<UserResponse> DuplicateEmail() =>
        ServiceResult<UserResponse>.Failure(ServiceErrorType.Conflict, "Email already exists.");
}
