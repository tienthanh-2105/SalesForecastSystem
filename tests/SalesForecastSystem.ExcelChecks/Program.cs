using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;

var connection = Environment.GetEnvironmentVariable("TEST_SQL_CONNECTION") ?? throw new Exception("Run through RunDisposableIntegration.ps1 -ExcelOnly.");
if (!new SqlConnectionStringBuilder(connection).InitialCatalog.StartsWith("SalesForecastTest_", StringComparison.Ordinal)) throw new Exception("Only disposable databases are allowed.");
await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
using var client = new HttpClient { BaseAddress = new Uri("http://localhost:5188"), Timeout = TimeSpan.FromSeconds(90) };
var root = Environment.GetEnvironmentVariable("TEST_PROJECT_ROOT")!;
var apiBin = Environment.GetEnvironmentVariable("TEST_API_BIN")!;
var password = "ExcelTest1!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
var tokens = new Dictionary<string, string>();
var checkedCount = 0;
var logs = new List<string>();
var apiDir = Path.Combine(Path.GetTempPath(), "SalesForecastExcelChecks", Guid.NewGuid().ToString("N"));
Process? host = null;

void Assert(bool condition, string label) { if (!condition) throw new Exception("FAIL " + label); checkedCount++; }
async Task<HttpResponseMessage> Request(string method, string url, string? role, HttpContent? content = null)
{
    using var request = new HttpRequestMessage(new HttpMethod(method), url) { Content = content };
    if (role is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens[role]);
    return await client.SendAsync(request);
}
async Task<JsonElement> Json(string method, string url, string? role, int status, object? body = null)
{
    using var response = await Request(method, url, role, body is null ? null : JsonContent.Create(body));
    var text = await response.Content.ReadAsStringAsync();
    Assert((int)response.StatusCode == status, $"{method} {url}: expected {status}, got {(int)response.StatusCode}: {text}");
    return JsonDocument.Parse(text).RootElement.Clone();
}
async Task<JsonElement> Upload(string route, byte[] bytes, string? role = "Admin", int expected = 200, string name = "data.xlsx")
{
    using var content = new MultipartFormDataContent();
    var file = new ByteArrayContent(bytes); file.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    content.Add(file, "file", name);
    using var response = await Request("POST", route, role, content);
    var text = await response.Content.ReadAsStringAsync();
    Assert((int)response.StatusCode == expected, $"{route}: expected {expected}, got {(int)response.StatusCode}: {text}");
    return string.IsNullOrWhiteSpace(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
}
async Task<byte[]> Download(string route, string? role = "Admin", int expected = 200)
{
    using var response = await Request("GET", route, role);
    Assert((int)response.StatusCode == expected, $"download {route}: {(int)response.StatusCode}: {(response.IsSuccessStatusCode ? "" : await response.Content.ReadAsStringAsync())}");
    if (response.IsSuccessStatusCode)
    {
        Assert(response.Content.Headers.ContentType?.MediaType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "XLSX content type");
        Assert(response.Content.Headers.ContentDisposition?.FileNameStar?.EndsWith(".xlsx") == true, "XLSX attachment filename");
    }
    return await response.Content.ReadAsByteArrayAsync();
}
string[] productHeaders = ["SKU", "Tên sản phẩm", "Mã danh mục", "Đơn vị", "Giá bán", "Tồn kho tối thiểu", "Đang kinh doanh", "Mô tả"];
string[] orderHeaders = ["Mã đơn hàng", "Ngày đặt", "Mã kho", "Tên khách hàng", "Số điện thoại", "Địa chỉ giao hàng", "SKU", "Số lượng", "Đơn giá", "Giảm giá (tiền)", "Trạng thái", "Ghi chú"];
byte[] Workbook(string sheetName, string[] headers, object?[][] rows, Action<IXLWorksheet>? change = null)
{
    using var workbook = new XLWorkbook(); var sheet = workbook.AddWorksheet(sheetName);
    for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
    for (var r = 0; r < rows.Length; r++) for (var c = 0; c < rows[r].Length; c++) sheet.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(rows[r][c]);
    change?.Invoke(sheet);
    using var output = new MemoryStream(); workbook.SaveAs(output); return output.ToArray();
}
bool HasError(JsonElement result, string column) => result.GetProperty("errors").EnumerateArray().Any(e => e.GetProperty("column").GetString() == column);

try
{
    foreach (var role in new[] { "Admin", "WarehouseManager", "SalesStaff" })
    {
        var roleId = await db.Roles.Where(r => r.Name == role).Select(r => r.RoleId).SingleAsync();
        db.Users.Add(new User { RoleId = roleId, FullName = "Excel " + role, Email = role.ToLowerInvariant() + "@excel.test", PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), Status = "Active", CreatedAt = DateTime.UtcNow });
    }
    var category = new Category { Code = "DM-EXCEL", Name = "Excel Leaf", IsActive = true };
    var warehouse = new Warehouse { Name = "Excel Warehouse", IsActive = true };
    var supplier = new Supplier { Name = "Excel Supplier", TaxCode = "0123456789", IsActive = true };
    db.AddRange(category, warehouse, supplier); await db.SaveChangesAsync();
    await db.Entry(category).ReloadAsync();
    Directory.CreateDirectory(apiDir);
    foreach (var source in Directory.EnumerateFiles(apiBin, "*", SearchOption.AllDirectories))
    {
        var target = Path.Combine(apiDir, Path.GetRelativePath(apiBin, source)); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(source, target);
    }
    var start = new ProcessStartInfo("dotnet") { WorkingDirectory = apiDir, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    start.ArgumentList.Add(Path.Combine(apiDir, "SalesForecastSystem.API.dll"));
    start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
    start.Environment["ASPNETCORE_URLS"] = client.BaseAddress!.ToString();
    start.Environment["ConnectionStrings__DefaultConnection"] = connection;
    start.Environment["Jwt__Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    start.Environment["Jwt__Issuer"] = "ExcelChecks"; start.Environment["Jwt__Audience"] = "ExcelChecks";
    start.Environment["SeedAdmin__Email"] = ""; start.Environment["SeedAdmin__Password"] = "";
    start.Environment["Logging__LogLevel__Default"] = "Warning";
    host = Process.Start(start)!;
    host.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (logs) logs.Add(e.Data); };
    host.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (logs) logs.Add(e.Data); };
    host.BeginOutputReadLine(); host.BeginErrorReadLine();
    var ready = false;
    for (var i = 0; i < 100; i++) { try { using var r = await client.GetAsync("/swagger/v1/swagger.json"); if (r.IsSuccessStatusCode) { ready = true; break; } } catch (HttpRequestException) { } await Task.Delay(100); }
    Assert(ready, "API ready: " + string.Join('\n', logs));
    foreach (var role in new[] { "Admin", "WarehouseManager", "SalesStaff" }) tokens[role] = (await Json("POST", "/api/auth/login", null, 200, new { email = role.ToLowerInvariant() + "@excel.test", password })).GetProperty("accessToken").GetString()!;

    var productFile = Workbook("SanPham", productHeaders, [["EXCEL-001", "Sản phẩm mới", category.Code, "Máy", 150000.25m, 0, true, "Mô tả đầy đủ"]]);
    await Upload("/api/products/import", productFile, null, 401);
    await Upload("/api/products/import", productFile, "SalesStaff", 403);
    await Upload("/api/sales/import", productFile, "WarehouseManager", 403);
    await Download("/api/sales/export", "WarehouseManager", 403);
    await Download("/api/products/export", null, 401);
    var template = await Download("/api/products/template");
    using (var book = new XLWorkbook(new MemoryStream(template))) Assert(book.Worksheet("SanPham").LastRowUsed()!.RowNumber() == 1, "template has no sample data to import");
    var preview = await Upload("/api/products/import/preview", productFile, "WarehouseManager");
    Assert(preview.GetProperty("isValid").GetBoolean() && preview.GetProperty("createdCount").GetInt32() == 0, "valid product preview: " + preview.GetRawText());
    Assert(!await db.Products.AnyAsync(p => p.SKU == "EXCEL-001"), "preview writes nothing");
    var imported = await Upload("/api/products/import", productFile, "WarehouseManager");
    Assert(imported.GetProperty("createdCount").GetInt32() == 1, "product created");
    var productId = imported.GetProperty("createdIds")[0].GetInt32();
    Assert((await db.Products.AsNoTracking().SingleAsync(p => p.ProductId == productId)).SalePrice == 150000.25m, "exact product decimal");
    Assert(HasError(await Upload("/api/products/import", productFile, expected: 400), "SKU"), "reimport existing SKU is blocked");
    var badFile = Workbook("SanPham", productHeaders, [["GOOD-ROLLBACK", "Good", category.Code, "Cái", 100], ["BAD-ROLLBACK", "Bad", category.Code, "Cái", -1]]);
    Assert(HasError(await Upload("/api/products/import", badFile, expected: 400), "Giá bán"), "row number/field validation");
    Assert(!await db.Products.AnyAsync(p => p.SKU == "GOOD-ROLLBACK"), "bad second row leaves no first product");
    var duplicateFile = Workbook("SanPham", productHeaders, [["DUP", "One", category.Code, "Cái", 100], [" dup ", "Two", category.Code, "Cái", 100]]);
    Assert((await Upload("/api/products/import", duplicateFile, expected: 400)).GetProperty("errors").EnumerateArray().Count(e => e.GetProperty("column").GetString() == "SKU") == 2, "both duplicate rows marked");
    var formulaFile = Workbook("SanPham", productHeaders, [["FORMULA", "One", category.Code, "Cái", 100]], sheet => sheet.Cell("E2").FormulaA1 = "1+1");
    Assert(HasError(await Upload("/api/products/import", formulaFile, expected: 400), "Giá bán"), "formulas not evaluated or saved");
    var missingFile = Workbook("SanPham", ["Tên sản phẩm"], [["Missing SKU"]]);
    Assert(HasError(await Upload("/api/products/import", missingFile, expected: 400), "SKU"), "missing headers");
    await Upload("/api/products/import", productFile, expected: 400, name: "wrong.csv");
    Assert(!(await Upload("/api/products/import/preview", [1, 2, 3])).GetProperty("isValid").GetBoolean(), "corrupt ZIP rejected");
    await Upload("/api/products/import", new byte[5 * 1024 * 1024 + 1], expected: 413);
    var tooMany = Workbook("SanPham", productHeaders, [], sheet => sheet.Cell(2002, 1).Value = "OVER-LIMIT");
    Assert(!(await Upload("/api/products/import/preview", tooMany)).GetProperty("isValid").GetBoolean(), "2000 row limit");

    var exported = await Download("/api/products/export?search=EXCEL-001");
    using (var book = new XLWorkbook(new MemoryStream(exported)))
    {
        Assert(book.Worksheet("SanPham").LastRowUsed()!.RowNumber() == 2, "product export respects filter");
        Assert(book.Worksheet("SanPham").Cell("H2").GetString() == "Mô tả đầy đủ", "export retains description");
    }
    var purchase = await Json("POST", "/api/purchases", "Admin", 201, new { orderNumber = "EXCEL-PO", warehouseId = warehouse.WarehouseId, supplierId = supplier.SupplierId, orderDate = "2026-10-05" });
    var purchaseId = purchase.GetProperty("purchaseOrderId").GetInt64();
    await Json("POST", $"/api/purchases/{purchaseId}/items", "Admin", 200, new { productId, quantity = 20, unitPrice = 100 });
    await Json("POST", $"/api/purchases/{purchaseId}/post", "Admin", 200);
    var secondProduct = await Upload("/api/products/import", Workbook("SanPham", productHeaders, [["EXCEL-002", "Second", category.Code, "Máy", 100]]));
    var secondProductId = secondProduct.GetProperty("createdIds")[0].GetInt32();
    var purchase2 = await Json("POST", "/api/purchases", "Admin", 201, new { orderNumber = "EXCEL-PO2", warehouseId = warehouse.WarehouseId, supplierId = supplier.SupplierId, orderDate = "2026-10-05" });
    var p2 = purchase2.GetProperty("purchaseOrderId").GetInt64();
    await Json("POST", $"/api/purchases/{p2}/items", "Admin", 200, new { productId = secondProductId, quantity = 10, unitPrice = 100 });
    await Json("POST", $"/api/purchases/{p2}/post", "Admin", 200);
    object?[] OrderRow(string number, string sku = "EXCEL-001", string name = "Khách nhập Excel", string phone = "0901234567", string status = "Draft", int quantity = 2) =>
        [number, "2026-10-05", $"KHO-{warehouse.WarehouseId:D4}", name, phone, "Địa chỉ mới", sku, quantity, 100m, 1m, status, "Ghi chú"];
    var orderFile = Workbook("DonHang", orderHeaders, [OrderRow("EXCEL-SO"), OrderRow("EXCEL-SO", "EXCEL-002")]);
    var stockBefore = await db.InventoryTransactions.SumAsync(t => (long)t.Quantity);
    var customerCount = await db.Customers.CountAsync();
    var orderPreview = await Upload("/api/sales/import/preview", orderFile, "SalesStaff");
    Assert(orderPreview.GetProperty("isValid").GetBoolean(), "two-line order preview valid");
    Assert(await db.Customers.CountAsync() == customerCount, "order preview creates no customer");
    var orderResult = await Upload("/api/sales/import", orderFile, "SalesStaff");
    Assert(orderResult.GetProperty("createdCount").GetInt32() == 1 && orderResult.GetProperty("totalRows").GetInt32() == 2, "grouped one order two lines");
    var orderId = orderResult.GetProperty("createdIds")[0].GetInt64();
    var order = await db.SalesOrders.AsNoTracking().Include(o => o.Items).SingleAsync(o => o.SalesOrderId == orderId);
    Assert(order.Status == "Draft" && order.Items.Count == 2 && order.CustomerPhone == "0901234567", "draft, phone and both details persisted");
    Assert(order.Items.Sum(i => i.LineTotal) == 398m, "exact discount amount");
    Assert(await db.InventoryTransactions.SumAsync(t => (long)t.Quantity) == stockBefore, "import draft changes no inventory");
    Assert(await db.Customers.CountAsync() == customerCount + 1, "one new customer reused for every row");
    await Upload("/api/sales/import", orderFile, expected: 400);
    Assert(await db.SalesOrders.CountAsync(o => o.OrderNumber == "EXCEL-SO") == 1, "reimport does not duplicate order");
    var invalidStatus = Workbook("DonHang", orderHeaders, [OrderRow("BAD-COMPLETED", status: "Completed")]);
    Assert(HasError(await Upload("/api/sales/import", invalidStatus, expected: 400), "Trạng thái"), "completed import forbidden");
    var invalidStock = Workbook("DonHang", orderHeaders, [OrderRow("BAD-STOCK", quantity: 999)]);
    Assert(HasError(await Upload("/api/sales/import", invalidStock, expected: 400), "Số lượng"), "stock validation matches sales business rules");
    var repeatedItem = Workbook("DonHang", orderHeaders, [OrderRow("BAD-ITEM"), OrderRow("BAD-ITEM")]);
    Assert(HasError(await Upload("/api/sales/import", repeatedItem, expected: 400), "SKU"), "repeated product in order blocked");
    var mismatch = Workbook("DonHang", orderHeaders, [OrderRow("BAD-COMMON"), OrderRow("BAD-COMMON", "EXCEL-002", name: "Khác tên")]);
    Assert(HasError(await Upload("/api/sales/import", mismatch, expected: 400), "Mã đơn hàng"), "header mismatch marks all lines");
    var salesExport = await Download("/api/sales/export?search=EXCEL-SO&status=Draft", "SalesStaff");
    using (var book = new XLWorkbook(new MemoryStream(salesExport)))
    {
        Assert(book.Worksheet("DonHang").LastRowUsed()!.RowNumber() == 3, "export includes every order item");
        Assert(book.Worksheet("DonHang").Cell("E2").GetString() == "0901234567", "export retains phone leading zero");
        Assert(book.Worksheet("DonHang").Cell("J2").GetValue<decimal>() == 1m, "export retains exact discount money");
    }
    await Upload("/api/sales/import", salesExport, expected: 400);

    // Force an actual database failure after customer/header insertion, not a preflight validation error.
    await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER dbo.TR_ExcelChecks_Failure ON dbo.SalesOrderItems AFTER INSERT AS BEGIN IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.SalesOrders o ON o.SalesOrderId=i.SalesOrderId WHERE o.OrderNumber='FORCED-ROLLBACK') THROW 51203, 'Injected test failure', 1; END;");
    try
    {
        var forced = Workbook("DonHang", orderHeaders, [OrderRow("FORCED-ROLLBACK", name: "Rollback customer", phone: "0988888888")]);
        await Upload("/api/sales/import", forced, expected: 409);
        Assert(!await db.SalesOrders.AnyAsync(o => o.OrderNumber == "FORCED-ROLLBACK"), "rollback removed header after SQL failure");
        Assert(!await db.Customers.AnyAsync(c => c.PhoneNumber == "0988888888"), "rollback removed customer after SQL failure");
    }
    finally { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER dbo.TR_ExcelChecks_Failure;"); }

    var raceFile = Workbook("SanPham", productHeaders, [["RACE-SKU", "Race", category.Code, "Cái", 100]]);
    async Task<int> Race()
    {
        using var body = new MultipartFormDataContent(); body.Add(new ByteArrayContent(raceFile), "file", "race.xlsx");
        using var response = await Request("POST", "/api/products/import", "Admin", body); return (int)response.StatusCode;
    }
    var race = await Task.WhenAll(Race(), Race());
    Assert(race.Count(s => s == 200) == 1 && race.All(s => s is 200 or 400 or 409), "one concurrent import succeeds");
    Assert(await db.Products.CountAsync(p => p.SKU == "RACE-SKU") == 1, "database prevents simultaneous duplicate SKU");
    Console.WriteLine($"ALL {checkedCount} EXCEL HTTP AND SQL ASSERTIONS PASSED.");
}
catch (Exception ex) { Console.Error.WriteLine(ex); Console.Error.WriteLine(string.Join('\n', logs)); throw; }
finally
{
    if (host is not null) { if (!host.HasExited) { host.Kill(true); await host.WaitForExitAsync(); } host.Dispose(); }
    var allowedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "SalesForecastExcelChecks")) + Path.DirectorySeparatorChar;
    if (Directory.Exists(apiDir) && Path.GetFullPath(apiDir).StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
        for (var attempt = 0; ; attempt++)
        {
            try { Directory.Delete(apiDir, true); break; }
            catch (Exception ex) when (attempt < 20 && ex is IOException or UnauthorizedAccessException) { await Task.Delay(100); }
        }
}
