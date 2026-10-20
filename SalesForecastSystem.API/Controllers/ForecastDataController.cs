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

[ApiController]
[Route("api/forecasts")]
[Authorize(Roles = RoleNames.All)]
public sealed class ForecastController(IForecastService service) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ForecastRunResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> RunAsync([FromBody] ForecastRunRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        var result = await service.RunAsync(request, userId, cancellationToken);
        return FromServiceResult(result, value => CreatedAtRoute("GetForecastRunById", new { runId = value.ForecastRunId }, value));
    }

    [HttpGet("{runId:long}", Name = "GetForecastRunById")]
    [ProducesResponseType(typeof(ForecastRunResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(long runId, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(runId, cancellationToken);
        return FromServiceResult(result, Ok);
    }
}
