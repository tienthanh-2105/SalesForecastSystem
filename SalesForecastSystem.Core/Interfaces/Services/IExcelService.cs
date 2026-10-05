using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Excel;
using SalesForecastSystem.Core.DTOs.Products;
using SalesForecastSystem.Core.DTOs.Sales;

namespace SalesForecastSystem.Core.Interfaces.Services;

public interface IExcelService
{
    Task<ExcelImportResponse> ImportAsync(ExcelResource resource, Stream file, bool preview, int userId, CancellationToken cancellationToken);
    Task<ServiceResult<ExcelFileResponse>> ExportProductsAsync(ProductQueryRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<ExcelFileResponse>> ExportSalesAsync(SalesQueryRequest request, CancellationToken cancellationToken);
    Task<ExcelFileResponse> TemplateAsync(ExcelResource resource, CancellationToken cancellationToken);
}
