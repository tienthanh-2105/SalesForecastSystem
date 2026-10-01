using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesForecastSystem.Core.DTOs.Forecasting;
using SalesForecastSystem.Core.Helpers;
using SalesForecastSystem.Core.Interfaces.Services;

namespace SalesForecastSystem.API.Controllers;

[ApiController]
[Route("api/forecast-data")]
[Authorize(Roles = RoleNames.All)]
public sealed class ForecastDataController(IForecastDataService service) : ApiControllerBase
{
    [HttpGet("daily")]
    [ProducesResponseType(typeof(ForecastDatasetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDailyDatasetAsync(
        [FromQuery] ForecastDataQuery request,
        CancellationToken cancellationToken)
    {
        var result = await service.GetDailyDatasetAsync(request, cancellationToken);
        return FromServiceResult(result, Ok);
    }
}
