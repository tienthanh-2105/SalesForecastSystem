using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesForecastSystem.Core.DTOs.Products;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.Helpers;
using SalesForecastSystem.Core.Interfaces.Services;

namespace SalesForecastSystem.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Roles = RoleNames.All)]
public sealed class ProductController(IProductService productService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProductListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPagedAsync(
        [FromQuery] ProductQueryRequest request,
        CancellationToken cancellationToken)
    {
        var products = await productService.GetPagedAsync(request, cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id:int}", Name = "GetProductById")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = await productService.GetByIdAsync(id, cancellationToken);
        return FromServiceResult(result, Ok);
    }

    [HttpGet("{id:int}/stock")]
    [ProducesResponseType(typeof(ProductStockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStockAsync(int id, CancellationToken cancellationToken)
    {
        var result = await productService.GetStockAsync(id, cancellationToken);
        return FromServiceResult(result, Ok);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.ProductManagers)]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(
        ProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productService.CreateAsync(request, cancellationToken);
        return FromServiceResult(
            result,
            product => CreatedAtRoute("GetProductById", new { id = product.ProductId }, product));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.ProductManagers)]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync(
        int id,
        ProductUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productService.UpdateAsync(id, request, cancellationToken);
        return FromServiceResult(result, Ok);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.ProductManagers)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        var result = await productService.DeactivateAsync(id, cancellationToken);
        return FromServiceResult(result, _ => Ok(new { message = "Product deactivated successfully." }));
    }
}
