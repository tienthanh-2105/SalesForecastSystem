using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Warehouses;
using SalesForecastSystem.Core.Helpers;
using SalesForecastSystem.Core.Interfaces.Services;

namespace SalesForecastSystem.API.Controllers;

[ApiController]
[Route("api/warehouses")]
[Authorize(Roles = RoleNames.All)]
public sealed class WarehouseController(IWarehouseService service) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<WarehouseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPagedAsync([FromQuery] WarehouseQueryRequest request, CancellationToken cancellationToken) =>
        Ok(await service.GetPagedAsync(request, cancellationToken));

    [HttpGet("{id:int}", Name = "GetWarehouseById")]
    [ProducesResponseType(typeof(WarehouseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        FromServiceResult(await service.GetByIdAsync(id, cancellationToken), Ok);

    [HttpPost]
    [Authorize(Roles = RoleNames.ProductManagers)]
    [ProducesResponseType(typeof(WarehouseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(WarehouseRequest request, CancellationToken cancellationToken) =>
        FromServiceResult(await service.CreateAsync(request, cancellationToken),
            item => CreatedAtRoute("GetWarehouseById", new { id = item.WarehouseId }, item));

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.ProductManagers)]
    [ProducesResponseType(typeof(WarehouseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync(int id, WarehouseRequest request, CancellationToken cancellationToken) =>
        FromServiceResult(await service.UpdateAsync(id, request, cancellationToken), Ok);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.ProductManagers)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateAsync(int id, CancellationToken cancellationToken) =>
        FromServiceResult(await service.DeactivateAsync(id, cancellationToken), _ => Ok(new { message = "Warehouse deactivated." }));
}
