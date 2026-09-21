using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Suppliers;
using SalesForecastSystem.Core.Helpers;
using SalesForecastSystem.Core.Interfaces.Services;

namespace SalesForecastSystem.API.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize(Roles = RoleNames.ProductManagers)]
public sealed class SupplierController(ISupplierService service) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SupplierResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPagedAsync([FromQuery] SupplierQueryRequest request, CancellationToken cancellationToken) =>
        Ok(await service.GetPagedAsync(request, cancellationToken));

    [HttpGet("{id:int}", Name = "GetSupplierById")]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        FromServiceResult(await service.GetByIdAsync(id, cancellationToken), Ok);

    [HttpPost]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(SupplierRequest request, CancellationToken cancellationToken) =>
        FromServiceResult(await service.CreateAsync(request, cancellationToken),
            item => CreatedAtRoute("GetSupplierById", new { id = item.SupplierId }, item));

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync(int id, SupplierRequest request, CancellationToken cancellationToken) =>
        FromServiceResult(await service.UpdateAsync(id, request, cancellationToken), Ok);

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateAsync(int id, CancellationToken cancellationToken) =>
        FromServiceResult(await service.DeactivateAsync(id, cancellationToken), _ => Ok(new { message = "Supplier deactivated." }));
}
