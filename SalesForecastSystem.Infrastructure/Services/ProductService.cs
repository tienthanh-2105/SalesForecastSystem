using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Products;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Services;

public sealed class ProductService(AppDbContext context) : IProductService
{
    public async Task<PagedResponse<ProductListItemResponse>> GetPagedAsync(
        ProductQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = context.Products
            .AsNoTracking()
            .Select(product => new ProductListRow
            {
                ProductId = product.ProductId,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.ParentCategory == null
                    ? product.Category.Name
                    : product.Category.ParentCategory.Name + " / " + product.Category.Name,
                SKU = product.SKU,
                Name = product.Name,
                ImageUrl = product.ImageUrl,
                Unit = product.Unit,
                SalePrice = product.SalePrice,
                MinimumStockLevel = product.MinimumStockLevel,
                QuantityOnHand = context.InventoryBalances
                    .Where(balance => balance.ProductId == product.ProductId)
                    .Sum(balance => (long?)balance.QuantityOnHand) ?? 0L,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            });

        var search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(product =>
                product.Name.Contains(search) || product.SKU.Contains(search));
        }

        if (request.CategoryId.HasValue)
        {
            var categoryId = request.CategoryId.Value;
            query = query.Where(product =>
                product.CategoryId == categoryId ||
                context.Categories.Any(category =>
                    category.CategoryId == product.CategoryId &&
                    category.ParentCategoryId == categoryId));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(product => product.IsActive == request.IsActive.Value);
        }

        query = request.StockStatus switch
        {
            ProductStockStatus.OutOfStock => query.Where(product => product.QuantityOnHand <= 0),
            ProductStockStatus.Low => query.Where(product =>
                product.QuantityOnHand > 0 && product.QuantityOnHand <= product.MinimumStockLevel),
            ProductStockStatus.Available => query.Where(product =>
                product.QuantityOnHand > product.MinimumStockLevel),
            _ => query
        };

        var totalItems = await query.CountAsync(cancellationToken);
        query = ApplySorting(query, request.SortBy, request.SortDirection);

        var skip = ((long)request.Page - 1) * request.PageSize;
        var rows = skip >= totalItems
            ? []
            : await query
                .Skip((int)skip)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

        var items = rows.Select(row => new ProductListItemResponse(
            row.ProductId,
            row.CategoryId,
            row.CategoryName,
            row.SKU,
            row.Name,
            row.ImageUrl,
            row.Unit,
            row.SalePrice,
            row.MinimumStockLevel,
            row.QuantityOnHand,
            GetStockStatus(row.QuantityOnHand, row.MinimumStockLevel),
            row.IsActive,
            row.CreatedAt,
            row.UpdatedAt)).ToList();

        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)request.PageSize);
        return new PagedResponse<ProductListItemResponse>(
            items,
            request.Page,
            request.PageSize,
            totalItems,
            totalPages);
    }

    public async Task<ServiceResult<ProductResponse>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var product = await context.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ProductId == id, cancellationToken);

        return product is null
            ? NotFound()
            : ServiceResult<ProductResponse>.Success(ToResponse(product));
    }

    public async Task<ServiceResult<ProductStockResponse>> GetStockAsync(
        int id,
        CancellationToken cancellationToken = default,
        int? warehouseId = null)
    {
        var exists = await context.Products.AnyAsync(
            product => product.ProductId == id,
            cancellationToken);
        if (!exists)
        {
            return ServiceResult<ProductStockResponse>.Failure(
                ServiceErrorType.NotFound,
                "Product was not found.");
        }

        var quantityOnHand = warehouseId.HasValue
            ? await context.Database.SqlQuery<long>($"SELECT COALESCE(SUM(CONVERT(bigint, Quantity)), CONVERT(bigint, 0)) AS [Value] FROM dbo.InventoryTransactions WHERE ProductId = {id} AND WarehouseId = {warehouseId.Value}").SingleAsync(cancellationToken)
            : await context.Database.SqlQuery<long>($"SELECT COALESCE(SUM(CONVERT(bigint, Quantity)), CONVERT(bigint, 0)) AS [Value] FROM dbo.InventoryTransactions WHERE ProductId = {id}").SingleAsync(cancellationToken);

        return ServiceResult<ProductStockResponse>.Success(new ProductStockResponse(id, quantityOnHand));
    }

    public async Task<ServiceResult<ProductResponse>> CreateAsync(
        ProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateReferencesAsync(request, null, cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        var product = new Product();
        ApplyRequest(product, request);
        context.Products.Add(product);

        var saveError = await SaveChangesAsync(cancellationToken);
        return saveError ?? ServiceResult<ProductResponse>.Success(ToResponse(product));
    }

    public async Task<ServiceResult<ProductResponse>> UpdateAsync(
        int id,
        ProductUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await context.Products
            .SingleOrDefaultAsync(item => item.ProductId == id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        if (!TryDecodeRowVersion(request.RowVersion, out var originalRowVersion))
        {
            return ServiceResult<ProductResponse>.Failure(
                ServiceErrorType.Validation,
                "Row version is invalid. Reload the product and try again.",
                nameof(request.RowVersion));
        }

        var validationError = await ValidateReferencesAsync(request, id, cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        context.Entry(product).Property(item => item.RowVersion).OriginalValue = originalRowVersion;
        ApplyRequest(product, request);
        var saveError = await SaveChangesAsync(cancellationToken);
        return saveError ?? ServiceResult<ProductResponse>.Success(ToResponse(product));
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var product = await context.Products
            .SingleOrDefaultAsync(item => item.ProductId == id, cancellationToken);
        if (product is null)
        {
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Product was not found.");
        }

        if (!product.IsActive)
        {
            return ServiceResult<bool>.Success(true);
        }

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<bool>.Failure(
                ServiceErrorType.Conflict,
                "Product was changed or deleted. Reload and try again.");
        }

        return ServiceResult<bool>.Success(true);
    }

    private async Task<ServiceResult<ProductResponse>?> ValidateReferencesAsync(
        ProductRequest request,
        int? productId,
        CancellationToken cancellationToken)
    {
        var category = await context.Categories
            .AsNoTracking()
            .Where(item => item.CategoryId == request.CategoryId)
            .Select(item => new
            {
                item.IsActive,
                HasChildren = item.Children.Any()
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (category is null)
        {
            return ServiceResult<ProductResponse>.Failure(
                ServiceErrorType.Validation,
                "Category does not exist.",
                nameof(request.CategoryId));
        }

        if (category.HasChildren)
        {
            return ServiceResult<ProductResponse>.Failure(
                ServiceErrorType.Validation,
                "Products must be assigned to a child category, not a parent category.",
                nameof(request.CategoryId));
        }

        if (!category.IsActive)
        {
            return ServiceResult<ProductResponse>.Failure(
                ServiceErrorType.Validation,
                "Products cannot be assigned to an inactive category.",
                nameof(request.CategoryId));
        }

        var normalizedSku = request.SKU.Trim();
        var skuExists = await context.Products.AnyAsync(
            product => product.SKU == normalizedSku &&
                       (!productId.HasValue || product.ProductId != productId.Value),
            cancellationToken);

        return skuExists ? Duplicate() : null;
    }

    private async Task<ServiceResult<ProductResponse>?> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<ProductResponse>.Failure(
                ServiceErrorType.Conflict,
                "Product was changed or deleted. Reload and try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Duplicate();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 547 })
        {
            return ServiceResult<ProductResponse>.Failure(
                ServiceErrorType.Conflict,
                "Related data changed or violated a database constraint. Reload and try again.");
        }

        return null;
    }

    private static void ApplyRequest(Product product, ProductRequest request)
    {
        product.CategoryId = request.CategoryId!.Value;
        product.SKU = request.SKU.Trim();
        product.Name = request.Name.Trim();
        product.Unit = request.Unit.Trim();
        product.Description = NormalizeOptional(request.Description);
        product.ImageUrl = NormalizeOptional(request.ImageUrl);
        product.SalePrice = request.SalePrice!.Value;
        product.MinimumStockLevel = request.MinimumStockLevel;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;
    }

    private static ProductResponse ToResponse(Product product) =>
        new(
            product.ProductId,
            product.CategoryId,
            product.SKU,
            product.Name,
            product.Unit,
            product.Description,
            product.ImageUrl,
            product.SalePrice,
            product.MinimumStockLevel,
            product.IsActive,
            product.CreatedAt,
            product.UpdatedAt,
            Convert.ToBase64String(product.RowVersion));

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IQueryable<ProductListRow> ApplySorting(
        IQueryable<ProductListRow> query,
        ProductSortBy sortBy,
        SortDirection direction)
    {
        return (sortBy, direction) switch
        {
            (ProductSortBy.Name, SortDirection.Desc) => query
                .OrderByDescending(product => product.Name)
                .ThenBy(product => product.ProductId),
            (ProductSortBy.SKU, SortDirection.Asc) => query
                .OrderBy(product => product.SKU)
                .ThenBy(product => product.ProductId),
            (ProductSortBy.SKU, SortDirection.Desc) => query
                .OrderByDescending(product => product.SKU)
                .ThenBy(product => product.ProductId),
            (ProductSortBy.SalePrice, SortDirection.Asc) => query
                .OrderBy(product => product.SalePrice)
                .ThenBy(product => product.ProductId),
            (ProductSortBy.SalePrice, SortDirection.Desc) => query
                .OrderByDescending(product => product.SalePrice)
                .ThenBy(product => product.ProductId),
            (ProductSortBy.CreatedAt, SortDirection.Asc) => query
                .OrderBy(product => product.CreatedAt)
                .ThenBy(product => product.ProductId),
            (ProductSortBy.CreatedAt, SortDirection.Desc) => query
                .OrderByDescending(product => product.CreatedAt)
                .ThenBy(product => product.ProductId),
            (ProductSortBy.QuantityOnHand, SortDirection.Asc) => query
                .OrderBy(product => product.QuantityOnHand)
                .ThenBy(product => product.ProductId),
            (ProductSortBy.QuantityOnHand, SortDirection.Desc) => query
                .OrderByDescending(product => product.QuantityOnHand)
                .ThenBy(product => product.ProductId),
            _ => query
                .OrderBy(product => product.Name)
                .ThenBy(product => product.ProductId)
        };
    }

    private static string GetStockStatus(long quantityOnHand, int minimumStockLevel) =>
        quantityOnHand <= 0
            ? nameof(ProductStockStatus.OutOfStock)
            : quantityOnHand <= minimumStockLevel
                ? nameof(ProductStockStatus.Low)
                : nameof(ProductStockStatus.Available);

    private static bool TryDecodeRowVersion(string? value, out byte[] rowVersion)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            rowVersion = [];
            return false;
        }

        try
        {
            rowVersion = Convert.FromBase64String(value);
            return rowVersion.Length == 8;
        }
        catch (FormatException)
        {
            rowVersion = [];
            return false;
        }
    }

    private static ServiceResult<ProductResponse> NotFound() =>
        ServiceResult<ProductResponse>.Failure(ServiceErrorType.NotFound, "Product was not found.");

    private static ServiceResult<ProductResponse> Duplicate() =>
        ServiceResult<ProductResponse>.Failure(ServiceErrorType.Conflict, "SKU already exists.");

    private sealed class ProductListRow
    {
        public int ProductId { get; init; }
        public int CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public string SKU { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? ImageUrl { get; init; }
        public string Unit { get; init; } = string.Empty;
        public decimal SalePrice { get; init; }
        public int MinimumStockLevel { get; init; }
        public long QuantityOnHand { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
