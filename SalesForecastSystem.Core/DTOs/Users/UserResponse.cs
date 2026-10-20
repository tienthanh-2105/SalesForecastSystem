namespace SalesForecastSystem.Core.DTOs.Users;

using SalesForecastSystem.Core.Enums;

public sealed record UserResponse(
    int UserId,
    string FullName,
    string Email,
    string? PhoneNumber,
    int RoleId,
    string RoleName,
    UserStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string Code);
