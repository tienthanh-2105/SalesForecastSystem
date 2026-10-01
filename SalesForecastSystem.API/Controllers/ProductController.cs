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
public sealed class ProductController(
    IProductService productService,
    IWebHostEnvironment environment) : ApiControllerBase
{
    private const long MaximumImageSize = 5 * 1024 * 1024;
    private const long MaximumUploadRequestSize = MaximumImageSize + 64 * 1024;

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

    [HttpPost("images")]
    [Authorize(Roles = RoleNames.ProductManagers)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumUploadRequestSize)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> UploadImageAsync(
        IFormFile image,
        CancellationToken cancellationToken)
    {
        if (image.Length == 0)
        {
            return BadRequest(new { message = "Vui lòng chọn ảnh cần tải lên." });
        }

        if (image.Length > MaximumImageSize)
        {
            return BadRequest(new { message = "Ảnh không được lớn hơn 5 MB." });
        }

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        var allowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };
        if (!allowedTypes.TryGetValue(extension, out var expectedContentType)
            || !string.Equals(image.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase)
            || !await HasValidImageSignatureAsync(image, extension, cancellationToken))
        {
            return BadRequest(new { message = "Chỉ chấp nhận ảnh JPG, PNG hoặc WebP hợp lệ." });
        }

        var uploadDirectory = Path.Combine(environment.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadDirectory);
        var fileName = $"{Guid.NewGuid():N}{(extension == ".jpeg" ? ".jpg" : extension)}";
        var filePath = Path.Combine(uploadDirectory, fileName);

        await using (var output = System.IO.File.Create(filePath))
        {
            await image.CopyToAsync(output, cancellationToken);
        }

        var imageUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/uploads/products/{fileName}";
        return Ok(new { imageUrl });
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

    private static async Task<bool> HasValidImageSignatureAsync(
        IFormFile image,
        string extension,
        CancellationToken cancellationToken)
    {
        var header = new byte[12];
        await using var stream = image.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

        if (extension is ".jpg" or ".jpeg")
        {
            return bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        }

        if (extension == ".png")
        {
            byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
            return bytesRead >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature);
        }

        return extension == ".webp"
            && bytesRead >= 12
            && header.AsSpan(0, 4).SequenceEqual("RIFF"u8)
            && header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
    }
}
