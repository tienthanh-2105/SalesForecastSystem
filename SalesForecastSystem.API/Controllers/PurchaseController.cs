using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Purchases;
using SalesForecastSystem.Core.Helpers;
using SalesForecastSystem.Core.Interfaces.Services;

namespace SalesForecastSystem.API.Controllers;

[ApiController]
[Route("api/purchases")]
[Authorize(Roles = RoleNames.ProductManagers)]
public sealed class PurchaseController(IPurchaseService service) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PurchaseListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedAsync([FromQuery] PurchaseQueryRequest request, CancellationToken cancellationToken) =>
        Ok(await service.GetPagedAsync(request, cancellationToken));

    [HttpGet("{id:long}", Name = "GetPurchaseById")]
    [ProducesResponseType(typeof(PurchaseResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        FromServiceResult(await service.GetByIdAsync(id, cancellationToken), Ok);

    [HttpPost]
    [ProducesResponseType(typeof(PurchaseResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync(PurchaseRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        return FromServiceResult(await service.CreateAsync(request, userId, cancellationToken),
            item => CreatedAtRoute("GetPurchaseById", new { id = item.PurchaseOrderId }, item));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateAsync(long id, PurchaseRequest request, CancellationToken cancellationToken) =>
        FromServiceResult(await service.UpdateAsync(id, request, cancellationToken), Ok);

    [HttpPost("{id:long}/items")]
    public async Task<IActionResult> AddItemAsync(long id, PurchaseItemRequest request, CancellationToken cancellationToken) =>
        FromServiceResult(await service.AddItemAsync(id, request, cancellationToken), Ok);

    [HttpPut("{id:long}/items/{itemId:long}")]
    public async Task<IActionResult> UpdateItemAsync(long id, long itemId, PurchaseItemRequest request, CancellationToken cancellationToken) =>
        FromServiceResult(await service.UpdateItemAsync(id, itemId, request, cancellationToken), Ok);

    [HttpDelete("{id:long}/items/{itemId:long}")]
    public async Task<IActionResult> DeleteItemAsync(long id, long itemId, CancellationToken cancellationToken) =>
        FromServiceResult(await service.DeleteItemAsync(id, itemId, cancellationToken), _ => NoContent());

    [HttpPost("{id:long}/post")]
    public async Task<IActionResult> PostAsync(long id, CancellationToken cancellationToken) =>
        FromServiceResult(await service.PostAsync(id, cancellationToken), Ok);

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> CancelAsync(long id, CancellationToken cancellationToken) =>
        FromServiceResult(await service.CancelAsync(id, cancellationToken), Ok);

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken) =>
        FromServiceResult(await service.DeleteAsync(id, cancellationToken), _ => NoContent());
}
