namespace SalesForecastSystem.Infrastructure.Entities;

public sealed class User
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Status { get; set; } = UserStatuses.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Role Role { get; set; } = null!;
    public ICollection<LoginSession> LoginSessions { get; set; } = [];
}

public static class UserStatuses
{
    public const string Active = "Active";
    public const string Locked = "Locked";
}
