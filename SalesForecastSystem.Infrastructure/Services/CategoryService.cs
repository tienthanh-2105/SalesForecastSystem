using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Categories;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Services;

public sealed class CategoryService(AppDbContext context) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.Categories
            .AsNoTracking()
            .OrderBy(category => category.CategoryId)
            .Select(category => new CategoryResponse(
                category.CategoryId,
                category.Name,
                category.Description,
                category.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<CategoryResponse>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var category = await context.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.CategoryId == id, cancellationToken);

        return category is null
            ? NotFound()
            : ServiceResult<CategoryResponse>.Success(ToResponse(category));
    }

    public async Task<ServiceResult<CategoryResponse>> CreateAsync(
        CategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = new Category();
        ApplyRequest(category, request);
        context.Categories.Add(category);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicate(exception))
        {
            return Duplicate();
        }

        return ServiceResult<CategoryResponse>.Success(ToResponse(category));
    }

    public async Task<ServiceResult<CategoryResponse>> UpdateAsync(
        int id,
        CategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await context.Categories
            .SingleOrDefaultAsync(item => item.CategoryId == id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        ApplyRequest(category, request);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound();
        }
        catch (DbUpdateException exception) when (IsDuplicate(exception))
        {
            return Duplicate();
        }

        return ServiceResult<CategoryResponse>.Success(ToResponse(category));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var category = await context.Categories
            .SingleOrDefaultAsync(item => item.CategoryId == id, cancellationToken);
        if (category is null)
        {
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Category was not found.");
        }

        context.Categories.Remove(category);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Category was not found.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 547 })
        {
            return ServiceResult<bool>.Failure(
                ServiceErrorType.Conflict,
                "Category contains products and cannot be deleted.");
        }

        return ServiceResult<bool>.Success(true);
    }

    private static void ApplyRequest(Category category, CategoryRequest request)
    {
        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
        category.IsActive = request.IsActive;
    }

    private static bool IsDuplicate(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private static CategoryResponse ToResponse(Category category) =>
        new(category.CategoryId, category.Name, category.Description, category.IsActive);

    private static ServiceResult<CategoryResponse> NotFound() =>
        ServiceResult<CategoryResponse>.Failure(ServiceErrorType.NotFound, "Category was not found.");

    private static ServiceResult<CategoryResponse> Duplicate() =>
        ServiceResult<CategoryResponse>.Failure(ServiceErrorType.Conflict, "Category name already exists.");
}
