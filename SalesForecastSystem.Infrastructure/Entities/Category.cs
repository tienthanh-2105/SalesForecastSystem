namespace SalesForecastSystem.Infrastructure.Entities;

public sealed class Category
{
    public int CategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Category? ParentCategory { get; set; }
    public ICollection<Category> Children { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
}
