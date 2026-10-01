using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Inventory;
using SalesForecastSystem.Core.Helpers;
using SalesForecastSystem.Core.Interfaces.Services;

namespace SalesForecastSystem.API.Controllers;

[ApiController]
[Route("api/inventory-transactions")]
[Authorize(Roles = RoleNames.All)]
public sealed class InventoryTransactionController(IInventoryTransactionService service) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<InventoryTransactionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedAsync([FromQuery] InventoryTransactionQuery request, CancellationToken cancellationToken) =>
        Ok(await service.GetPagedAsync(request, cancellationToken));
}
