using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Forecasting;

namespace SalesForecastSystem.Core.Interfaces.Services;

public interface IForecastService
{
    Task<ServiceResult<ForecastRunResponse>> RunAsync(ForecastRunRequest request, int userId, CancellationToken cancellationToken = default);
    Task<ServiceResult<ForecastRunResponse>> GetAsync(long runId, CancellationToken cancellationToken = default);
}
