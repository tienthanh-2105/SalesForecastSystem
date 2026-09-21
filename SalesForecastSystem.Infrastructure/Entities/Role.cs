namespace SalesForecastSystem.Infrastructure.Entities;

public sealed class Role
{
    public int RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public ICollection<User> Users { get; set; } = [];
}
