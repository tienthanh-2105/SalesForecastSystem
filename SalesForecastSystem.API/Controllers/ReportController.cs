using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesForecastSystem.Core.DTOs.Reports;
using SalesForecastSystem.Core.Helpers;
using SalesForecastSystem.Core.Interfaces.Services;

namespace SalesForecastSystem.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = RoleNames.All)]
public sealed class ReportController(IReportService service) : ControllerBase
{
    [HttpGet("sales-summary")]
    [ProducesResponseType(typeof(SalesSummaryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalesSummaryAsync(
        [FromQuery] SalesReportQuery request,
        CancellationToken cancellationToken) =>
        Ok(await service.GetSalesSummaryAsync(request, cancellationToken));

    [HttpGet("sales-trend")]
    [ProducesResponseType(typeof(IReadOnlyList<SalesTrendPointResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalesTrendAsync(
        [FromQuery] SalesTrendQuery request,
        CancellationToken cancellationToken) =>
        Ok(await service.GetSalesTrendAsync(request, cancellationToken));

    [HttpGet("top-products")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductSalesResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopProductsAsync(
        [FromQuery] SalesRankingQuery request,
        CancellationToken cancellationToken) =>
        Ok(await service.GetTopProductsAsync(request, cancellationToken));

    [HttpGet("top-categories")]
    [ProducesResponseType(typeof(IReadOnlyList<CategorySalesResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopCategoriesAsync(
        [FromQuery] SalesRankingQuery request,
        CancellationToken cancellationToken) =>
        Ok(await service.GetTopCategoriesAsync(request, cancellationToken));

    [HttpGet("sales-by-staff")]
    [ProducesResponseType(typeof(IReadOnlyList<StaffSalesResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalesByStaffAsync(
        [FromQuery] SalesRankingQuery request,
        CancellationToken cancellationToken) =>
        Ok(await service.GetSalesByStaffAsync(request, cancellationToken));

    [HttpGet("inventory")]
    [ProducesResponseType(typeof(IReadOnlyList<InventoryReportResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryAsync(
        [FromQuery] InventoryReportQuery request,
        CancellationToken cancellationToken) =>
        Ok(await service.GetInventoryAsync(request, cancellationToken));
}
