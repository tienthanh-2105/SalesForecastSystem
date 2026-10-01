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
            .OrderBy(category => category.ParentCategoryId.HasValue)
            .ThenBy(category => category.ParentCategoryId)
            .ThenBy(category => category.Name)
            .Select(ToResponseExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<CategoryResponse>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var category = await context.Categories
            .AsNoTracking()
            .Where(item => item.CategoryId == id)
            .Select(ToResponseExpression())
            .SingleOrDefaultAsync(cancellationToken);

        return category is null
            ? NotFound()
            : ServiceResult<CategoryResponse>.Success(category);
    }

    public async Task<ServiceResult<CategoryResponse>> CreateAsync(
        CategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateParentAsync(
            request.ParentCategoryId,
            null,
            cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        var category = new Category
        {
            Code = await GetNextCodeAsync(cancellationToken)
        };
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

        return await GetByIdAsync(category.CategoryId, cancellationToken);
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

        var validationError = await ValidateParentAsync(
            request.ParentCategoryId,
            id,
            cancellationToken);
        if (validationError is not null)
        {
            return validationError;
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

        return await GetByIdAsync(category.CategoryId, cancellationToken);
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
                "Category contains products or child categories and cannot be deleted.");
        }

        return ServiceResult<bool>.Success(true);
    }

    private static void ApplyRequest(Category category, CategoryRequest request)
    {
        category.ParentCategoryId = request.ParentCategoryId;
        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
        category.IsActive = request.IsActive;
    }

    private static bool IsDuplicate(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private async Task<ServiceResult<CategoryResponse>?> ValidateParentAsync(
        int? parentCategoryId,
        int? categoryId,
        CancellationToken cancellationToken)
    {
        if (!parentCategoryId.HasValue)
        {
            return null;
        }

        if (parentCategoryId == categoryId)
        {
            return Validation("A category cannot be its own parent.");
        }

        var parent = await context.Categories
            .AsNoTracking()
            .Where(category => category.CategoryId == parentCategoryId.Value)
            .Select(category => new
            {
                category.ParentCategoryId,
                HasProducts = category.Products.Any()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (parent is null)
        {
            return Validation("Parent category does not exist.");
        }

        if (parent.ParentCategoryId.HasValue)
        {
            return Validation("Only a root category can be selected as the parent.");
        }

        if (parent.HasProducts)
        {
            return Validation("A category containing products cannot become a parent category.");
        }

        if (categoryId.HasValue && await context.Categories.AnyAsync(
                category => category.ParentCategoryId == categoryId.Value,
                cancellationToken))
        {
            return Validation("A category containing child categories cannot be nested under another category.");
        }

        return null;
    }

    private static System.Linq.Expressions.Expression<Func<Category, CategoryResponse>> ToResponseExpression() =>
        category => new CategoryResponse(
            category.CategoryId,
            category.Code,
            category.Name,
            category.Description,
            category.IsActive,
            category.ParentCategoryId,
            category.ParentCategory == null ? null : category.ParentCategory.Name,
            category.Children.Any());

    private async Task<string> GetNextCodeAsync(CancellationToken cancellationToken)
    {
        var codes = await context.Categories
            .AsNoTracking()
            .Select(category => category.Code)
            .ToListAsync(cancellationToken);
        var usedNumbers = codes
            .Where(code => code.StartsWith("DM-"))
            .Select(code => int.TryParse(code.AsSpan(3), out var number) ? number : 0)
            .Where(number => number > 0)
            .ToHashSet();
        var nextNumber = 1;
        while (usedNumbers.Contains(nextNumber))
        {
            nextNumber++;
        }

        return $"DM-{nextNumber:0000}";
    }

    private static ServiceResult<CategoryResponse> NotFound() =>
        ServiceResult<CategoryResponse>.Failure(ServiceErrorType.NotFound, "Category was not found.");

    private static ServiceResult<CategoryResponse> Duplicate() =>
        ServiceResult<CategoryResponse>.Failure(ServiceErrorType.Conflict, "Category name already exists.");

    private static ServiceResult<CategoryResponse> Validation(string message) =>
        ServiceResult<CategoryResponse>.Failure(
            ServiceErrorType.Validation,
            message,
            nameof(CategoryRequest.ParentCategoryId));
}
