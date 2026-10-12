using SalesForecastSystem.Core.DTOs.Reports;
using SalesForecastSystem.Core.Common;

namespace SalesForecastSystem.Core.Interfaces.Services;

public interface IReportService
{
    Task<StockAlertsResponse> GetStockAlertsAsync(CancellationToken cancellationToken = default);
    Task<ProfitReportResponse> GetProfitAsync(SalesTrendQuery request, CancellationToken cancellationToken = default);
    Task<ServiceResult<StaffGroupSalesResponse>> GetSalesByGroupAsync(StaffGroupSalesQuery request, CancellationToken cancellationToken = default);
    Task<SalesSummaryResponse> GetSalesSummaryAsync(SalesReportQuery request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesTrendPointResponse>> GetSalesTrendAsync(SalesTrendQuery request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductSalesResponse>> GetTopProductsAsync(SalesRankingQuery request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategorySalesResponse>> GetTopCategoriesAsync(SalesRankingQuery request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StaffSalesResponse>> GetSalesByStaffAsync(SalesRankingQuery request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryReportResponse>> GetInventoryAsync(InventoryReportQuery request, CancellationToken cancellationToken = default);
}
