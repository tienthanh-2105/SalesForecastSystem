using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Excel;
using SalesForecastSystem.Core.DTOs.Products;
using SalesForecastSystem.Core.DTOs.Sales;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;
using SalesForecastSystem.Infrastructure.Excel;

namespace SalesForecastSystem.Infrastructure.Services;

public sealed class ExcelService(AppDbContext context, IProductService products, ISalesService sales) : IExcelService
{
    public async Task<ExcelImportResponse> ImportAsync(ExcelResource resource, Stream file, bool preview, int userId, CancellationToken ct)
    {
        var parsed = await ExcelWorkbook.ReadAsync(resource, file, ct);
        var rows = parsed.Rows; var errors = parsed.Errors;
        if (rows.Count == 0) return Response(rows, errors);
        // Re-read all references inside the same serializable transaction used for writes.
        // Preview never saves, and commit never trusts an earlier preview response.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            ValidateFields(resource, rows, errors);
            var skus = rows.Select(r => ExcelWorkbook.Key(r["sku"])).Distinct().ToArray();
            var storedProducts = await context.Products.AsNoTracking().Where(p => skus.Contains(p.SKU)).ToListAsync(ct);
            var productMap = storedProducts.ToDictionary(p => ExcelWorkbook.Key(p.SKU));
            if (resource == ExcelResource.Products)
            {
                var codes = rows.Select(r => ExcelWorkbook.Key(r["categoryCode"])).Distinct().ToArray();
                var categories = await context.Categories.AsNoTracking().Where(c => codes.Contains(c.Code))
                    .Select(c => new { c.CategoryId, c.Code, c.IsActive, HasChildren = context.Categories.Any(child => child.ParentCategoryId == c.CategoryId) }).ToListAsync(ct);
                var categoryMap = categories.ToDictionary(c => ExcelWorkbook.Key(c.Code));
                foreach (var row in rows)
                {
                    if (productMap.ContainsKey(ExcelWorkbook.Key(row["sku"]))) errors.Add(new(row.Number, "SKU", "SKU đã tồn tại trong hệ thống."));
                    if (!categoryMap.TryGetValue(ExcelWorkbook.Key(row["categoryCode"]), out var category) || !category.IsActive || category.HasChildren)
                        errors.Add(new(row.Number, "Mã danh mục", "Danh mục phải đang hoạt động và không có danh mục con."));
                }
                if (errors.Count != 0 || preview) return Response(rows, errors);
                var created = rows.Select(row => new Product
                {
                    SKU = row["sku"], Name = row["name"], Unit = row["unit"], Description = Optional(row["description"]),
                    CategoryId = categoryMap[ExcelWorkbook.Key(row["categoryCode"])].CategoryId,
                    SalePrice = MoneyValue(row["salePrice"]), MinimumStockLevel = string.IsNullOrEmpty(row["minimumStockLevel"]) ? 0 : int.Parse(row["minimumStockLevel"], CultureInfo.InvariantCulture),
                    IsActive = string.IsNullOrEmpty(row["isActive"]) || ExcelWorkbook.Key(row["isActive"]) == "TRUE", UpdatedAt = DateTime.UtcNow
                }).ToList();
                context.Products.AddRange(created);
                await context.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
                return Response(rows, errors, created.Select(p => (long)p.ProductId).ToArray());
            }

            var orderNumbers = rows.Select(r => ExcelWorkbook.Key(r["orderNumber"])).Distinct().ToArray();
            var existingOrders = await context.SalesOrders.AsNoTracking().Where(o => orderNumbers.Contains(o.OrderNumber)).Select(o => o.OrderNumber).ToListAsync(ct);
            var existingKeys = existingOrders.Select(ExcelWorkbook.Key).ToHashSet();
            var warehouseIds = rows.Select(r => WarehouseId(r["warehouseCode"])).Distinct().ToArray();
            var warehouses = await context.Warehouses.AsNoTracking().Where(w => warehouseIds.Contains(w.WarehouseId)).ToListAsync(ct);
            var warehouseMap = warehouses.ToDictionary(w => w.WarehouseId);
            var productIds = storedProducts.Select(p => p.ProductId).ToArray();
            var balances = await context.InventoryTransactions.AsNoTracking().Where(t => warehouseIds.Contains(t.WarehouseId) && productIds.Contains(t.ProductId))
                .GroupBy(t => new { t.WarehouseId, t.ProductId }).Select(g => new { g.Key.WarehouseId, g.Key.ProductId, Quantity = g.Sum(t => (long)t.Quantity) }).ToListAsync(ct);
            var stock = balances.ToDictionary(b => (b.WarehouseId, b.ProductId), b => b.Quantity);
            var phones = rows.Select(r => r["customerPhone"]).Distinct().ToArray();
            var customers = await context.Customers.AsNoTracking().Where(c => phones.Contains(c.PhoneNumber!)).ToListAsync(ct);
            var customerMap = customers.GroupBy(c => c.PhoneNumber?.Trim() ?? "").ToDictionary(g => g.Key, g => g.ToList());
            foreach (var row in rows)
            {
                if (existingKeys.Contains(ExcelWorkbook.Key(row["orderNumber"]))) errors.Add(new(row.Number, "Mã đơn hàng", "Mã đơn hàng đã tồn tại trong hệ thống."));
                var warehouseId = WarehouseId(row["warehouseCode"]);
                if (!warehouseMap.TryGetValue(warehouseId, out var warehouse) || !warehouse.IsActive)
                    errors.Add(new(row.Number, "Mã kho", "Kho không tồn tại hoặc đã ngừng hoạt động."));
                if (!productMap.TryGetValue(ExcelWorkbook.Key(row["sku"]), out var product) || !product.IsActive)
                    errors.Add(new(row.Number, "SKU", "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh."));
                else if (int.TryParse(row["quantity"], out var quantity) && quantity > stock.GetValueOrDefault((warehouseId, product.ProductId)))
                    errors.Add(new(row.Number, "Số lượng", "Số lượng vượt tồn kho hiện tại của kho đã chọn."));
                if (customerMap.TryGetValue(row["customerPhone"], out var matches) && (matches.Count != 1 || !matches[0].IsActive || !string.Equals(matches[0].FullName.Trim(), row["customerName"], StringComparison.OrdinalIgnoreCase)))
                    errors.Add(new(row.Number, "Số điện thoại", "Khách hàng theo số điện thoại không xác định duy nhất, đã ngừng hoạt động hoặc khác tên. Hãy kiểm tra thông tin khách hàng."));
            }
            foreach (var group in rows.GroupBy(r => r["customerPhone"]))
                if (group.Select(r => r["customerName"]).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                    foreach (var row in group) errors.Add(new(row.Number, "Tên khách hàng", "Một số điện thoại có nhiều tên khách hàng trong file."));
            if (errors.Count != 0 || preview) return Response(rows, errors);

            var selectedCustomers = new Dictionary<string, Customer>();
            foreach (var row in rows.DistinctBy(r => r["customerPhone"]))
            {
                var customer = customerMap.TryGetValue(row["customerPhone"], out var matches) ? matches[0] : new Customer
                { FullName = row["customerName"], PhoneNumber = row["customerPhone"], Address = row["shippingAddress"], IsActive = true };
                if (customer.CustomerId == 0) context.Customers.Add(customer);
                selectedCustomers[row["customerPhone"]] = customer;
            }
            await context.SaveChangesAsync(ct);
            var newOrders = rows.GroupBy(r => ExcelWorkbook.Key(r["orderNumber"])).Select(group =>
            {
                var first = group.First();
                return new SalesOrder
                {
                    OrderNumber = first["orderNumber"], WarehouseId = WarehouseId(first["warehouseCode"]),
                    CustomerId = selectedCustomers[first["customerPhone"]].CustomerId, CreatedByUserId = userId,
                    OrderDate = DateTime.ParseExact(first["orderDate"], "yyyy-MM-dd", CultureInfo.InvariantCulture), Status = "Draft",
                    CustomerName = first["customerName"], CustomerPhone = first["customerPhone"], ShippingAddress = first["shippingAddress"], Notes = Optional(first["notes"]),
                    Items = group.Select(r => new SalesOrderItem { ProductId = productMap[ExcelWorkbook.Key(r["sku"])].ProductId, Quantity = int.Parse(r["quantity"], CultureInfo.InvariantCulture), UnitPrice = MoneyValue(r["unitPrice"]), Discount = string.IsNullOrEmpty(r["discount"]) ? 0 : MoneyValue(r["discount"]) }).ToList()
                };
            }).ToList();
            context.SalesOrders.AddRange(newOrders);
            await context.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
            return Response(rows, errors, newOrders.Select(o => o.SalesOrderId).ToArray());
        }
        catch (Exception ex) when (IsDatabaseConflict(ex))
        {
            await transaction.RollbackAsync(CancellationToken.None); context.ChangeTracker.Clear();
            errors.Add(new(0, "File", "Dữ liệu đã thay đổi đồng thời hoặc vi phạm ràng buộc. Không có dòng nào được lưu. Hãy kiểm tra và thử lại."));
            return Response(rows, errors) with { Conflict = true };
        }
    }

    private static void ValidateFields(ExcelResource resource, List<ExcelRow> rows, List<ExcelRowError> errors)
    {
        foreach (var row in rows)
        {
            void Error(string column, string message) => errors.Add(new(row.Number, column, message));
            void Length(string key, string label, int max) { if (row[key].Length > max) Error(label, $"Tối đa {max} ký tự."); }
            foreach (var column in ExcelWorkbook.Columns(resource).Where(c => c.Required)) if (string.IsNullOrWhiteSpace(row[column.Key])) Error(column.Label, "Bắt buộc.");
            Length("sku", "SKU", 50);
            if (!Regex.IsMatch(row["sku"], @"^[\x20-\x7E]+$")) Error("SKU", "SKU chỉ nhận ký tự ASCII in được.");
            if (resource == ExcelResource.Products)
            {
                Length("name", "Tên sản phẩm", 200); Length("unit", "Đơn vị", 30); Length("description", "Mô tả", 1000);
                if (!ExcelWorkbook.Money(row["salePrice"], out _)) Error("Giá bán", "Số từ 0 đến 9999999999999.99, tối đa 2 chữ số thập phân.");
                if (row["minimumStockLevel"] != "" && (!Regex.IsMatch(row["minimumStockLevel"], @"^\d+$") || !int.TryParse(row["minimumStockLevel"], out _))) Error("Tồn kho tối thiểu", "Số nguyên không âm, tối đa 2147483647.");
                if (row["isActive"] != "" && ExcelWorkbook.Key(row["isActive"]) is not ("TRUE" or "FALSE")) Error("Đang kinh doanh", "Chỉ nhận TRUE hoặc FALSE.");
            }
            else
            {
                Length("orderNumber", "Mã đơn hàng", 50); Length("customerName", "Tên khách hàng", 100); Length("shippingAddress", "Địa chỉ giao hàng", 255); Length("notes", "Ghi chú", 500);
                if (!Regex.IsMatch(row["orderNumber"], @"^[\x20-\x7E]+$")) Error("Mã đơn hàng", "Mã đơn chỉ nhận ký tự ASCII in được.");
                if (!DateTime.TryParseExact(row["orderDate"], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || date < new DateTime(1753, 1, 1)) Error("Ngày đặt", "Ngày hợp lệ theo YYYY-MM-DD, từ 1753-01-01.");
                if (!Regex.IsMatch(row["customerPhone"], @"^\d{8,15}$")) Error("Số điện thoại", "Cần 8 đến 15 chữ số.");
                var quantity = 0;
                if (!Regex.IsMatch(row["quantity"], @"^\d+$") || !int.TryParse(row["quantity"], out quantity) || quantity <= 0) Error("Số lượng", "Số nguyên dương, tối đa 2147483647.");
                if (!ExcelWorkbook.Money(row["unitPrice"], out var price)) Error("Đơn giá", "Số không âm, tối đa 2 chữ số thập phân và 9999999999999.99.");
                if (row["discount"] != "" && !ExcelWorkbook.Money(row["discount"], out _)) Error("Giảm giá (tiền)", "Số không âm, tối đa 2 chữ số thập phân.");
                if (decimal.TryParse(row["discount"], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var discount) && discount > quantity * price) Error("Giảm giá (tiền)", "Không được vượt giá trị dòng hàng.");
                if (row["status"] != "" && row["status"] != "Draft") Error("Trạng thái", "Đơn mới chỉ nhận Draft (Nháp).");
            }
        }
        foreach (var group in rows.GroupBy(r => resource == ExcelResource.Products ? ExcelWorkbook.Key(r["sku"]) : ExcelWorkbook.Key(r["orderNumber"]) + "\0" + ExcelWorkbook.Key(r["sku"])).Where(g => g.Count() > 1))
            foreach (var row in group) errors.Add(new(row.Number, "SKU", resource == ExcelResource.Products ? "SKU trùng trong file." : "Sản phẩm bị lặp trong cùng đơn hàng."));
        if (resource == ExcelResource.Sales)
        {
            foreach (var group in rows.GroupBy(r => ExcelWorkbook.Key(r["orderNumber"])))
                if (new[] { "orderDate", "warehouseCode", "customerName", "customerPhone", "shippingAddress", "notes", "status" }.Any(k => group.Select(r => k == "status" && r[k] == "" ? "Draft" : k == "warehouseCode" ? ExcelWorkbook.Key(r[k]) : r[k]).Distinct().Count() > 1))
                    foreach (var row in group) errors.Add(new(row.Number, "Mã đơn hàng", "Thông tin chung không thống nhất giữa các dòng của cùng đơn hàng."));
        }
    }

    private static ExcelImportResponse Response(List<ExcelRow> rows, List<ExcelRowError> errors, long[]? ids = null) =>
        new(rows.Count, errors.Any(e => e.Row <= 1) ? 0 : rows.Count(r => errors.All(e => e.Row != r.Number)), errors, ids?.Length ?? 0, ids);
    private static string? Optional(string text) => string.IsNullOrWhiteSpace(text) ? null : text;
    private static decimal MoneyValue(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
    private static int WarehouseId(string code) => Regex.IsMatch(ExcelWorkbook.Key(code), @"^KHO-\d+$") && int.TryParse(code[4..], out var id) && id > 0 && ExcelWorkbook.Key(code) == $"KHO-{id:D4}" ? id : 0;
    private static bool IsDatabaseConflict(Exception ex) => (ex as SqlException ?? ex.InnerException as SqlException)?.Number is 2601 or 2627 or 547 or 1205 or 1222 or 51202 or 51203 or 51204;

    public async Task<ServiceResult<ExcelFileResponse>> ExportProductsAsync(ProductQueryRequest request, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        request.Page = 1; request.PageSize = 100;
        var list = await products.GetPagedAsync(request, ct);
        if (list.TotalItems > ExcelWorkbook.MaxRows) return TooLarge();
        var ids = list.Items.Select(p => p.ProductId).ToList();
        for (var page = 2; page <= list.TotalPages; page++) { request.Page = page; ids.AddRange((await products.GetPagedAsync(request, ct)).Items.Select(p => p.ProductId)); }
        var items = await context.Products.AsNoTracking().Where(p => ids.Contains(p.ProductId)).ToListAsync(ct);
        var categories = await context.Categories.AsNoTracking().ToListAsync(ct);
        var rows = ids.Select(id => items.Single(p => p.ProductId == id)).Select(p => new Dictionary<string, object?> { ["sku"] = p.SKU, ["name"] = p.Name, ["categoryCode"] = categories.Single(c => c.CategoryId == p.CategoryId).Code, ["unit"] = p.Unit, ["salePrice"] = p.SalePrice, ["minimumStockLevel"] = p.MinimumStockLevel, ["isActive"] = p.IsActive, ["description"] = p.Description }).ToList();
        return ServiceResult<ExcelFileResponse>.Success(ExcelWorkbook.Write(ExcelResource.Products, rows, await ReferencesAsync(ExcelResource.Products, ct), false));
    }

    public async Task<ServiceResult<ExcelFileResponse>> ExportSalesAsync(SalesQueryRequest request, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        request.Page = 1; request.PageSize = 100;
        var list = await sales.GetPagedAsync(request, ct);
        if (list.TotalItems > ExcelWorkbook.MaxRows) return TooLarge();
        var ids = list.Items.Select(o => o.SalesOrderId).ToList();
        for (var page = 2; page <= list.TotalPages; page++) { request.Page = page; ids.AddRange((await sales.GetPagedAsync(request, ct)).Items.Select(o => o.SalesOrderId)); }
        var orders = await context.SalesOrders.AsNoTracking().Include(o => o.Items).Where(o => ids.Contains(o.SalesOrderId)).ToListAsync(ct);
        if (orders.Sum(o => o.Items.Count) > ExcelWorkbook.MaxRows) return TooLarge();
        if (orders.Any(o => o.Items.Count == 0)) return ServiceResult<ExcelFileResponse>.Failure(ServiceErrorType.Validation, "Có đơn chưa có chi tiết sản phẩm; hãy hoàn thiện đơn hoặc thu hẹp bộ lọc.");
        var productIds = orders.SelectMany(o => o.Items.Select(i => i.ProductId)).Distinct().ToArray();
        var skus = await context.Products.AsNoTracking().Where(p => productIds.Contains(p.ProductId)).ToDictionaryAsync(p => p.ProductId, p => p.SKU, ct);
        var rows = new List<Dictionary<string, object?>>();
        foreach (var order in ids.Select(id => orders.Single(o => o.SalesOrderId == id)))
            foreach (var item in order.Items.OrderBy(i => i.SalesOrderItemId))
                rows.Add(new() { ["orderNumber"] = order.OrderNumber, ["orderDate"] = order.OrderDate.ToString("yyyy-MM-dd"), ["warehouseCode"] = $"KHO-{order.WarehouseId:D4}", ["customerName"] = order.CustomerName, ["customerPhone"] = order.CustomerPhone, ["shippingAddress"] = order.ShippingAddress, ["sku"] = skus[item.ProductId], ["quantity"] = item.Quantity, ["unitPrice"] = item.UnitPrice, ["discount"] = item.Discount, ["status"] = order.Status, ["notes"] = order.Notes });
        return ServiceResult<ExcelFileResponse>.Success(ExcelWorkbook.Write(ExcelResource.Sales, rows, await ReferencesAsync(ExcelResource.Sales, ct), false));
    }

    public async Task<ExcelFileResponse> TemplateAsync(ExcelResource resource, CancellationToken ct) => ExcelWorkbook.Write(resource, [], await ReferencesAsync(resource, ct), true);
    private async Task<List<(string Kind, string Code, string Name)>> ReferencesAsync(ExcelResource resource, CancellationToken ct)
    {
        var refs = new List<(string, string, string)>();
        if (resource == ExcelResource.Products)
        {
            var categories = await context.Categories.AsNoTracking().Where(c => c.IsActive && !context.Categories.Any(child => child.ParentCategoryId == c.CategoryId)).OrderBy(c => c.CategoryId).ToListAsync(ct);
            refs.AddRange(categories.Select(c => ("Danh mục", c.Code, c.Name)));
        }
        else
        {
            var warehouses = await context.Warehouses.AsNoTracking().Where(w => w.IsActive).OrderBy(w => w.WarehouseId).ToListAsync(ct);
            var productRefs = await context.Products.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.ProductId).Select(p => new { p.SKU, p.Name }).ToListAsync(ct);
            refs.AddRange(warehouses.Select(w => ("Kho", $"KHO-{w.WarehouseId:D4}", w.Name))); refs.AddRange(productRefs.Select(p => ("Sản phẩm", p.SKU, p.Name)));
        }
        return refs;
    }
    private static ServiceResult<ExcelFileResponse> TooLarge() => ServiceResult<ExcelFileResponse>.Failure(ServiceErrorType.Validation, "Vượt quá 2000 dòng. Hãy thu hẹp bộ lọc trước khi xuất.");
}
