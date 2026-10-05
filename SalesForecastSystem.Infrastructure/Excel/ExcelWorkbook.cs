using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using SalesForecastSystem.Core.DTOs.Excel;

namespace SalesForecastSystem.Infrastructure.Excel;

internal sealed record ExcelColumn(string Key, string Label, bool Required, bool Text = false);
internal sealed class ExcelRow(int number)
{
    public int Number { get; } = number;
    public Dictionary<string, string> Values { get; } = [];
    public string this[string key] => Values.GetValueOrDefault(key, "").Trim();
}
internal sealed record ParsedExcel(List<ExcelRow> Rows, List<ExcelRowError> Errors);

internal static partial class ExcelWorkbook
{
    public const int MaxRows = 2000;
    public const int MaxBytes = 5 * 1024 * 1024;
    public static string Sheet(ExcelResource resource) => resource == ExcelResource.Products ? "SanPham" : "DonHang";
    public static ExcelColumn[] Columns(ExcelResource resource) => resource == ExcelResource.Products ?
    [new("sku", "SKU", true, true), new("name", "Tên sản phẩm", true), new("categoryCode", "Mã danh mục", true, true),
     new("unit", "Đơn vị", true), new("salePrice", "Giá bán", true), new("minimumStockLevel", "Tồn kho tối thiểu", false),
     new("isActive", "Đang kinh doanh", false), new("description", "Mô tả", false)] :
    [new("orderNumber", "Mã đơn hàng", true, true), new("orderDate", "Ngày đặt", true), new("warehouseCode", "Mã kho", true, true),
     new("customerName", "Tên khách hàng", true), new("customerPhone", "Số điện thoại", true, true), new("shippingAddress", "Địa chỉ giao hàng", true),
     new("sku", "SKU", true, true), new("quantity", "Số lượng", true), new("unitPrice", "Đơn giá", true),
     new("discount", "Giảm giá (tiền)", false), new("status", "Trạng thái", false, true), new("notes", "Ghi chú", false)];

    public static async Task<ParsedExcel> ReadAsync(ExcelResource resource, Stream input, CancellationToken ct)
    {
        var result = new ParsedExcel([], []);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await input.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > MaxBytes) { result.Errors.Add(new(0, "File", "File vượt quá 5 MB.")); return result; }
            await buffer.WriteAsync(chunk.AsMemory(0, read), ct);
        }
        try
        {
            buffer.Position = 0;
            using (var zip = new ZipArchive(buffer, ZipArchiveMode.Read, true))
            {
                // Bound decompression before asking the workbook library to load XML.
                if (zip.Entries.Count > 10000 || zip.Entries.Sum(e => e.Length) > 32 * 1024 * 1024L)
                    throw new InvalidDataException();
                if (zip.GetEntry("xl/workbook.xml") is null || zip.Entries.Any(e => e.FullName.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException();
            }
            buffer.Position = 0;
            using var workbook = new XLWorkbook(buffer);
            if (!workbook.TryGetWorksheet(Sheet(resource), out var sheet)) { result.Errors.Add(new(1, "Sheet", $"Thiếu sheet {Sheet(resource)}.")); return result; }
            var last = sheet.LastRowUsed()?.RowNumber() ?? 1;
            if (last > MaxRows + 1 || (sheet.LastColumnUsed()?.ColumnNumber() ?? 0) > 32)
            { result.Errors.Add(new(0, "File", "Tối đa 2000 dòng dữ liệu và 32 cột.")); return result; }
            var columns = Columns(resource);
            var positions = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var cell in sheet.Row(1).CellsUsed())
            {
                if (cell.HasFormula || cell.IsMerged() || cell.DataType != XLDataType.Text)
                { result.Errors.Add(new(1, "Tiêu đề", "Tiêu đề phải là văn bản, không gộp ô hoặc dùng công thức.")); continue; }
                var label = cell.GetString().Trim().TrimEnd('*').TrimEnd();
                if (!columns.Any(c => c.Label == label)) result.Errors.Add(new(1, label, "Cột không có trong file mẫu."));
                if (!positions.TryAdd(label, cell.Address.ColumnNumber)) result.Errors.Add(new(1, label, "Cột bị lặp."));
            }
            foreach (var column in columns.Where(c => c.Required && !positions.ContainsKey(c.Label))) result.Errors.Add(new(1, column.Label, "Thiếu cột bắt buộc."));
            if (result.Errors.Count != 0) return result;
            for (var i = 2; i <= last; i++)
            {
                ct.ThrowIfCancellationRequested();
                if (sheet.Row(i).CellsUsed().All(c => c.IsEmpty())) continue;
                var row = new ExcelRow(i);
                foreach (var column in columns)
                {
                    if (!positions.TryGetValue(column.Label, out var index)) { row.Values[column.Key] = ""; continue; }
                    var cell = sheet.Cell(i, index);
                    if (cell.HasFormula || cell.IsMerged() || cell.HasHyperlink || cell.DataType == XLDataType.Error)
                    { result.Errors.Add(new(i, column.Label, "Không nhận công thức, ô lỗi, liên kết hoặc ô gộp.")); row.Values[column.Key] = ""; continue; }
                    if (column.Text && !cell.IsEmpty() && cell.DataType != XLDataType.Text)
                        result.Errors.Add(new(i, column.Label, "Phải là văn bản để giữ nguyên mã và số 0 đầu."));
                    if (column.Key is "salePrice" or "unitPrice" or "discount" && cell.Style.NumberFormat.Format.Contains('%'))
                        result.Errors.Add(new(i, column.Label, "Nhập số tiền, không dùng định dạng Percentage."));
                    row.Values[column.Key] = cell.DataType switch
                    {
                        XLDataType.DateTime when column.Key == "orderDate" => cell.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        XLDataType.Number => Convert.ToDecimal(cell.GetDouble()).ToString(CultureInfo.InvariantCulture),
                        XLDataType.Boolean => cell.GetBoolean() ? "TRUE" : "FALSE",
                        _ => cell.GetString().Trim()
                    };
                }
                result.Rows.Add(row);
            }
            if (result.Rows.Count == 0) result.Errors.Add(new(0, "File", "File chưa có dữ liệu."));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            result.Rows.Clear(); result.Errors.Clear(); result.Errors.Add(new(0, "File", "Không đọc được file .xlsx hợp lệ, hoặc file quá lớn sau giải nén. Không dùng file có mật khẩu."));
        }
        return result;
    }

    public static ExcelFileResponse Write(ExcelResource resource, List<Dictionary<string, object?>> rows, List<(string Kind, string Code, string Name)> references, bool template)
    {
        using var workbook = new XLWorkbook();
        var columns = Columns(resource);
        var sheet = workbook.AddWorksheet(Sheet(resource));
        for (var i = 0; i < columns.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = columns[i].Label;
            sheet.Column(i + 1).Width = columns[i].Key is "description" or "shippingAddress" or "notes" ? 40 : 24;
            if (columns[i].Text) sheet.Column(i + 1).Style.NumberFormat.Format = "@";
        }
        for (var r = 0; r < rows.Count; r++)
            for (var c = 0; c < columns.Length; c++) sheet.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(rows[r].GetValueOrDefault(columns[c].Key));
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Row(1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, Math.Max(1, rows.Count + 1), columns.Length).SetAutoFilter();
        var reference = workbook.AddWorksheet("ThamChieu");
        reference.Cell(1, 1).Value = "Loại"; reference.Cell(1, 2).Value = "Mã"; reference.Cell(1, 3).Value = "Tên";
        reference.Column(2).Style.NumberFormat.Format = "@";
        for (var i = 0; i < references.Count; i++) { reference.Cell(i + 2, 1).Value = references[i].Kind; reference.Cell(i + 2, 2).Value = references[i].Code; reference.Cell(i + 2, 3).Value = references[i].Name; }
        reference.Columns(1, 3).Width = 32;
        var guide = workbook.AddWorksheet("HuongDan");
        guide.Cell(1, 1).Value = "Cột"; guide.Cell(1, 2).Value = "Bắt buộc";
        for (var i = 0; i < columns.Length; i++) { guide.Cell(i + 2, 1).Value = columns[i].Label; guide.Cell(i + 2, 2).Value = columns[i].Required ? "Có" : "Không"; }
        var notes = new[] { $"Điền dữ liệu vào sheet {Sheet(resource)}, từ dòng 2; giữ nguyên tiêu đề. Tối đa 5 MB, 2000 dòng.", "Mã và số điện thoại phải là văn bản. Ngày YYYY-MM-DD; giá tiền không âm, tối đa 2 chữ số thập phân.", "Đơn hàng mới chỉ nhận Draft. Giảm giá là số tiền của cả dòng; không nhập tồn kho hiện tại.", "SKU/mã đơn đã có bị từ chối. Một dòng lỗi khiến cả file không được lưu. Không nhập lại file xuất để ghi đè dữ liệu.", "Mỗi dòng đơn hàng là một sản phẩm. Thông tin chung của các dòng cùng mã đơn phải giống nhau.", "Đơn hàng cần sản phẩm hoạt động, kho hoạt động và số lượng không vượt tồn kho hiện tại, giống biểu mẫu tạo đơn." };
        for (var i = 0; i < notes.Length; i++) guide.Cell(columns.Length + 3 + i, 1).Value = notes[i];
        guide.Column(1).Width = 100; guide.Column(1).Style.Alignment.WrapText = true; guide.Column(2).Width = 15;
        using var output = new MemoryStream(); workbook.SaveAs(output);
        return new ExcelFileResponse(output.ToArray(), template ? $"Mau_{Sheet(resource)}.xlsx" : $"{Sheet(resource)}_{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
    }

    public static string Key(string value) => value.Trim().ToUpperInvariant();
    public static bool Money(string value, out decimal amount)
    {
        amount = 0;
        return MoneyPattern().IsMatch(value) && decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount) && amount is >= 0 and <= 9999999999999.99m;
    }
    [GeneratedRegex(@"^\d+(?:\.\d{1,2})?$")] private static partial Regex MoneyPattern();
}
