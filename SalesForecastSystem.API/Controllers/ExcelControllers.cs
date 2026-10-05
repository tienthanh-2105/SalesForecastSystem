using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesForecastSystem.Core.DTOs.Excel;
using SalesForecastSystem.Core.DTOs.Products;
using SalesForecastSystem.Core.DTOs.Sales;
using SalesForecastSystem.Core.Helpers;
using SalesForecastSystem.Core.Interfaces.Services;

namespace SalesForecastSystem.API.Controllers;

public sealed class ExcelUploadRequest
{
    [Required] public IFormFile File { get; set; } = null!;
}

public abstract class ExcelControllerBase(IExcelService excel) : ApiControllerBase
{
    protected IExcelService Excel { get; } = excel;
    protected const long UploadLimit = 5 * 1024 * 1024 + 64 * 1024;
    protected const string ExcelMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    protected IActionResult Download(ExcelFileResponse file) => File(file.Content, ExcelMime, file.FileName);
    protected async Task<IActionResult> Upload(ExcelResource resource, ExcelUploadRequest request, bool preview, CancellationToken ct)
    {
        if (request.File.Length > 5 * 1024 * 1024) return Problem(statusCode: 413, title: "File vượt quá 5 MB.");
        if (request.File.Length == 0 || !Path.GetExtension(request.File.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Cần file .xlsx không rỗng." });
        if (!int.TryParse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        await using var stream = request.File.OpenReadStream();
        var response = await Excel.ImportAsync(resource, stream, preview, userId, ct);
        return response.Conflict ? Conflict(response) : !preview && !response.IsValid ? BadRequest(response) : Ok(response);
    }
}

[ApiController]
[Route("api/products")]
[Authorize(Roles = RoleNames.All)]
public sealed class ProductExcelController(IExcelService excel) : ExcelControllerBase(excel)
{
    [HttpGet("template")]
    public async Task<IActionResult> Template(CancellationToken ct) => Download(await Excel.TemplateAsync(ExcelResource.Products, ct));

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] ProductQueryRequest request, CancellationToken ct) =>
        FromServiceResult(await Excel.ExportProductsAsync(request, ct), Download);

    [HttpPost("import/preview")]
    [Authorize(Roles = RoleNames.ProductManagers)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(UploadLimit), RequestFormLimits(MultipartBodyLengthLimit = UploadLimit)]
    [ProducesResponseType(typeof(ExcelImportResponse), 200)]
    public Task<IActionResult> Preview([FromForm] ExcelUploadRequest request, CancellationToken ct) => Upload(ExcelResource.Products, request, true, ct);

    [HttpPost("import")]
    [Authorize(Roles = RoleNames.ProductManagers)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(UploadLimit), RequestFormLimits(MultipartBodyLengthLimit = UploadLimit)]
    [ProducesResponseType(typeof(ExcelImportResponse), 200)]
    [ProducesResponseType(typeof(ExcelImportResponse), 400)]
    [ProducesResponseType(typeof(ExcelImportResponse), 409)]
    public Task<IActionResult> Import([FromForm] ExcelUploadRequest request, CancellationToken ct) => Upload(ExcelResource.Products, request, false, ct);
}

[ApiController]
[Route("api/sales")]
[Authorize(Roles = RoleNames.Admin + "," + RoleNames.SalesStaff)]
public sealed class SalesExcelController(IExcelService excel) : ExcelControllerBase(excel)
{
    [HttpGet("template")]
    public async Task<IActionResult> Template(CancellationToken ct) => Download(await Excel.TemplateAsync(ExcelResource.Sales, ct));

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] SalesQueryRequest request, CancellationToken ct) =>
        FromServiceResult(await Excel.ExportSalesAsync(request, ct), Download);

    [HttpPost("import/preview")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(UploadLimit), RequestFormLimits(MultipartBodyLengthLimit = UploadLimit)]
    [ProducesResponseType(typeof(ExcelImportResponse), 200)]
    public Task<IActionResult> Preview([FromForm] ExcelUploadRequest request, CancellationToken ct) => Upload(ExcelResource.Sales, request, true, ct);

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(UploadLimit), RequestFormLimits(MultipartBodyLengthLimit = UploadLimit)]
    [ProducesResponseType(typeof(ExcelImportResponse), 200)]
    [ProducesResponseType(typeof(ExcelImportResponse), 400)]
    [ProducesResponseType(typeof(ExcelImportResponse), 409)]
    public Task<IActionResult> Import([FromForm] ExcelUploadRequest request, CancellationToken ct) => Upload(ExcelResource.Sales, request, false, ct);
}
