using SalesForecastSystem.Core.DTOs.Reports;

namespace SalesForecastSystem.Core.Interfaces.Services;

public interface IReportService
{
    Task<SalesSummaryResponse> GetSalesSummaryAsync(SalesReportQuery request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesTrendPointResponse>> GetSalesTrendAsync(SalesTrendQuery request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductSalesResponse>> GetTopProductsAsync(SalesRankingQuery request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategorySalesResponse>> GetTopCategoriesAsync(SalesRankingQuery request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StaffSalesResponse>> GetSalesByStaffAsync(SalesRankingQuery request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryReportResponse>> GetInventoryAsync(InventoryReportQuery request, CancellationToken cancellationToken = default);
}
