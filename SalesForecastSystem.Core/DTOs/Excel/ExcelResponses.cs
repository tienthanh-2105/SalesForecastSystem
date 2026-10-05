namespace SalesForecastSystem.Core.DTOs.Excel;

public enum ExcelResource { Products, Sales }
public sealed record ExcelRowError(int Row, string Column, string Message);
public sealed record ExcelImportResponse(int TotalRows, int ValidRows, IReadOnlyList<ExcelRowError> Errors,
    int CreatedCount = 0, IReadOnlyList<long>? CreatedIds = null, bool Conflict = false)
{
    public bool IsValid => Errors.Count == 0;
}
public sealed record ExcelFileResponse(byte[] Content, string FileName);
