using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Forecasting;

namespace SalesForecastSystem.Core.Interfaces.Services;

public interface IForecastDataService
{
    Task<ServiceResult<ForecastDatasetResponse>> GetDailyDatasetAsync(
        ForecastDataQuery request,
        CancellationToken cancellationToken = default);
}
