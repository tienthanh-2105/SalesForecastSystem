namespace SalesForecastSystem.Core.DTOs.Categories;

public sealed record CategoryResponse(
    int CategoryId,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    int? ParentCategoryId,
    string? ParentCategoryName,
    bool HasChildren);
