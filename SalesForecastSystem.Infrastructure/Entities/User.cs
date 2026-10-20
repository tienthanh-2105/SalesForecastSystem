namespace SalesForecastSystem.Infrastructure.Entities;

using SalesForecastSystem.Core.Enums;

public sealed class User
{
    public int UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Role Role { get; set; } = null!;
    public ICollection<LoginSession> LoginSessions { get; set; } = [];
}
