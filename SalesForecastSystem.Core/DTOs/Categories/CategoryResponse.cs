namespace SalesForecastSystem.Core.DTOs.Categories;

public sealed record CategoryResponse(
    int CategoryId,
    string Name,
    string? Description,
    bool IsActive);
