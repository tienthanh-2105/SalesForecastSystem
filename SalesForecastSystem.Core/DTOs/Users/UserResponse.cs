namespace SalesForecastSystem.Core.DTOs.Users;

public sealed record UserResponse(
    int UserId,
    string FullName,
    string Email,
    string? PhoneNumber,
    int RoleId,
    string RoleName,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
