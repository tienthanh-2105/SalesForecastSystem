using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Sales;
using SalesForecastSystem.Core.Helpers;
using SalesForecastSystem.Core.Interfaces.Services;

namespace SalesForecastSystem.API.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize(Roles = RoleNames.Admin + "," + RoleNames.SalesStaff)]
public sealed class SalesController(ISalesService service) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SalesListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedAsync([FromQuery] SalesQueryRequest request, CancellationToken cancellationToken) =>
        Ok(await service.GetPagedAsync(request, cancellationToken));

    [HttpGet("{id:long}", Name = "GetSalesById")]
    [ProducesResponseType(typeof(SalesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        FromServiceResult(await service.GetByIdAsync(id, cancellationToken), Ok);

    [HttpPost]
    [ProducesResponseType(typeof(SalesResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync(SalesRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        return FromServiceResult(await service.CreateAsync(request, userId, cancellationToken),
            item => CreatedAtRoute("GetSalesById", new { id = item.SalesOrderId }, item));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateAsync(long id, SalesRequest request, CancellationToken cancellationToken) =>
        FromServiceResult(await service.UpdateAsync(id, request, cancellationToken), Ok);

    [HttpPost("{id:long}/items")]
    public async Task<IActionResult> AddItemAsync(long id, SalesItemRequest request, CancellationToken cancellationToken) =>
        FromServiceResult(await service.AddItemAsync(id, request, cancellationToken), Ok);

    [HttpPut("{id:long}/items/{itemId:long}")]
    public async Task<IActionResult> UpdateItemAsync(long id, long itemId, SalesItemRequest request, CancellationToken cancellationToken) =>
        FromServiceResult(await service.UpdateItemAsync(id, itemId, request, cancellationToken), Ok);

    [HttpDelete("{id:long}/items/{itemId:long}")]
    public async Task<IActionResult> DeleteItemAsync(long id, long itemId, CancellationToken cancellationToken) =>
        FromServiceResult(await service.DeleteItemAsync(id, itemId, cancellationToken), _ => NoContent());

    [HttpPost("{id:long}/complete")]
    public async Task<IActionResult> CompleteAsync(long id, CancellationToken cancellationToken) =>
        FromServiceResult(await service.CompleteAsync(id, cancellationToken), Ok);

    [HttpPost("{id:long}/submit")]
    public async Task<IActionResult> SubmitAsync(long id, CancellationToken cancellationToken) =>
        FromServiceResult(await service.SubmitAsync(id, cancellationToken), Ok);

    [HttpPost("{id:long}/dispatch")]
    public async Task<IActionResult> DispatchAsync(long id, CancellationToken cancellationToken) =>
        FromServiceResult(await service.DispatchAsync(id, cancellationToken), Ok);

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> CancelAsync(long id, CancellationToken cancellationToken) =>
        FromServiceResult(await service.CancelAsync(id, cancellationToken), Ok);

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken) =>
        FromServiceResult(await service.DeleteAsync(id, cancellationToken), _ => NoContent());
}
