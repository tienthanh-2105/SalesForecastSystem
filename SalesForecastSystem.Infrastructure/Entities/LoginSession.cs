namespace SalesForecastSystem.Infrastructure.Entities;

public sealed class LoginSession
{
    public Guid SessionId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public User User { get; set; } = null!;
}
