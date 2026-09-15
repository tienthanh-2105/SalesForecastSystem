using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
var connection = Environment.GetEnvironmentVariable("TEST_SQL_CONNECTION")
    ?? @"Server=.\SQLEXPRESS;Database=SalesForecastingDB;Trusted_Connection=True;TrustServerCertificate=True";
await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
var tag = "check-" + Guid.NewGuid().ToString("N");
var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
var accounts = new Dictionary<string, NguoiDung>();
var tokens = new Dictionary<string, string>();
var apiLog = new List<string>();
var baseUrl = "http://localhost:5187";
using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
Process? api = null;
string? apiHostDirectory = null;
var passed = 0;

async Task StartApi()
{
    var sourceDirectory = Path.Combine(root, "SalesForecastSystem.API/bin/Debug/net8.0");
    apiHostDirectory = Path.Combine(Path.GetTempPath(), "SalesForecastSystem.IntegrationChecks", Guid.NewGuid().ToString("N"));
    foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
    {
        var destinationFile = Path.Combine(apiHostDirectory, Path.GetRelativePath(sourceDirectory, sourceFile));
        Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
        File.Copy(sourceFile, destinationFile);
    }

    var start = new ProcessStartInfo("dotnet")
    {
        WorkingDirectory = apiHostDirectory,
        UseShellExecute = false, CreateNoWindow = true,
        RedirectStandardOutput = true, RedirectStandardError = true
    };
    start.ArgumentList.Add(Path.Combine(apiHostDirectory, "SalesForecastSystem.API.dll"));
    start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
    start.Environment["ASPNETCORE_URLS"] = baseUrl;
    start.Environment["ConnectionStrings__DefaultConnection"] = connection;
    start.Environment["Jwt__Key"] = key;
    start.Environment["Jwt__Issuer"] = "IntegrationChecks";
    start.Environment["Jwt__Audience"] = "IntegrationChecks";
    start.Environment["SeedAdmin__Email"] = "";
    start.Environment["SeedAdmin__Password"] = "";
    start.Environment["Logging__LogLevel__Default"] = "Warning";
    api = Process.Start(start) ?? throw new Exception("Cannot start API.");
    api.OutputDataReceived += (_, e) => { if (e.Data != null) lock (apiLog) apiLog.Add(e.Data); };
    api.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (apiLog) apiLog.Add(e.Data); };
    api.BeginOutputReadLine(); api.BeginErrorReadLine();
    for (var attempt = 0; attempt < 100; attempt++)
    {
        if (api.HasExited) throw new Exception("API exited: " + string.Join('\n', apiLog));
        try
        {
            using var response = await client.GetAsync("/swagger/v1/swagger.json");
            if (response.IsSuccessStatusCode) return;
        }
        catch (HttpRequestException) { }
        await Task.Delay(100);
    }
    throw new Exception("API did not become ready.");
}

async Task StopApi()
{
    if (api is not null)
    {
        if (!api.HasExited) { api.Kill(entireProcessTree: true); await api.WaitForExitAsync(); }
        api.Dispose(); api = null;
    }
    if (apiHostDirectory is not null && Directory.Exists(apiHostDirectory))
    {
        var allowedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "SalesForecastSystem.IntegrationChecks")) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(apiHostDirectory).StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Test host cleanup path is outside the temporary test directory.");
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                Directory.Delete(apiHostDirectory, recursive: true);
                break;
            }
            catch (UnauthorizedAccessException) when (attempt < 20)
            {
                await Task.Delay(100);
            }
            catch (IOException) when (attempt < 20)
            {
                await Task.Delay(100);
            }
        }
        apiHostDirectory = null;
    }
}

async Task<JsonElement> Check(string label, HttpMethod method, string url, int expected, string? token = null, object? body = null)
{
    using var request = new HttpRequestMessage(method, url);
    if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    if (body is not null) request.Content = JsonContent.Create(body);
    using var response = await client.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();
    if ((int)response.StatusCode != expected)
        throw new Exception($"FAIL {label}: expected {expected}, got {(int)response.StatusCode}: {content}");
    passed++;
    Console.WriteLine($"PASS {label}: {expected}");
    return string.IsNullOrWhiteSpace(content) ? default : JsonDocument.Parse(content).RootElement.Clone();
}

async Task<string> Login(string role) => (await Check("login " + role, HttpMethod.Post, "/api/auth/login", 200,
    body: new { email = accounts[role].Email.ToUpperInvariant(), password })).GetProperty("accessToken").GetString()!;

string SignedToken(string original, string signingKey, string issuer, DateTime expiry)
{
    var parsed = new JwtSecurityTokenHandler().ReadJwtToken(original);
    var claims = parsed.Claims.Where(c => c.Type is "sub" or "jti" or "email" or "name" or "role");
    return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer, "IntegrationChecks", claims,
        DateTime.UtcNow.AddHours(-1), expiry,
        new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256)));
}

try
{
    var roleMap = new Dictionary<string, string> { ["Admin"] = "Admin", ["QuanLyKho"] = "Quản lý kho", ["NhanVienBanHang"] = "Nhân viên bán hàng" };
    foreach (var (role, name) in roleMap)
    {
        var roleId = await db.VaiTros.Where(x => x.TenVaiTro == name || x.TenVaiTro == role).Select(x => x.MaVaiTro).FirstAsync();
        var user = new NguoiDung
        {
            MaVaiTro = roleId, HoTen = "Kiểm thử " + role, Email = $"{tag}-{role}@example.invalid".ToLowerInvariant(),
            MatKhau = BCrypt.Net.BCrypt.HashPassword(password, 4), NgayTao = DateTime.UtcNow
        };
        accounts.Add(role, user); db.NguoiDungs.Add(user);
    }
    await db.SaveChangesAsync();
    await StartApi();

    await Check("anonymous list", HttpMethod.Get, "/api/danh-muc", 401);
    await Check("anonymous detail", HttpMethod.Get, "/api/danh-muc/0", 401);
    await Check("anonymous create", HttpMethod.Post, "/api/danh-muc", 401, body: new { tenDanhMuc = tag });
    await Check("anonymous update", HttpMethod.Put, "/api/danh-muc/0", 401, body: new { tenDanhMuc = tag });
    await Check("anonymous delete", HttpMethod.Delete, "/api/danh-muc/0", 401);
    await Check("anonymous logout", HttpMethod.Post, "/api/auth/logout", 401);
    await Check("invalid email", HttpMethod.Post, "/api/auth/login", 400, body: new { email = "bad", password });
    await Check("wrong password", HttpMethod.Post, "/api/auth/login", 401, body: new { email = accounts["Admin"].Email, password = "incorrect" });
    await Check("unknown email", HttpMethod.Post, "/api/auth/login", 401, body: new { email = $"{tag}@example.invalid", password });
    foreach (var role in accounts.Keys)
    {
        tokens[role] = await Login(role);
        var me = await Check("me " + role, HttpMethod.Get, "/api/auth/me", 200, tokens[role]);
        if (me.GetProperty("vaiTro").GetString() != role) throw new Exception("Role claim not normalized.");
        await Check("list " + role, HttpMethod.Get, "/api/danh-muc", 200, tokens[role]);
        if (role != "Admin")
        {
            await Check("create forbidden " + role, HttpMethod.Post, "/api/danh-muc", 403, tokens[role], new { tenDanhMuc = tag });
            await Check("update forbidden " + role, HttpMethod.Put, "/api/danh-muc/0", 403, tokens[role], new { tenDanhMuc = tag });
            await Check("delete forbidden " + role, HttpMethod.Delete, "/api/danh-muc/0", 403, tokens[role]);
        }
    }
    foreach (var (role, token) in tokens)
    {
        foreach (var target in new[] { "Admin", "QuanLyKho", "NhanVienBanHang" })
        {
            var route = target switch { "Admin" => "admin", "QuanLyKho" => "warehouse", _ => "sales" };
            await Check($"access {role} -> {target}", HttpMethod.Get, "/api/access-check/" + route,
                role == target ? 200 : 403, token);
        }
    }
    await Check("anonymous role endpoint", HttpMethod.Get, "/api/access-check/admin", 401);
    var admin = tokens["Admin"];
    await Check("invalid token", HttpMethod.Get, "/api/danh-muc", 401, "invalid");
    await Check("expired signed token", HttpMethod.Get, "/api/danh-muc", 401, SignedToken(admin, key, "IntegrationChecks", DateTime.UtcNow.AddMinutes(-1)));
    await Check("wrong issuer", HttpMethod.Get, "/api/danh-muc", 401, SignedToken(admin, key, "Other", DateTime.UtcNow.AddMinutes(5)));
    await Check("wrong signature", HttpMethod.Get, "/api/danh-muc", 401, SignedToken(admin, new string('x', 48), "IntegrationChecks", DateTime.UtcNow.AddMinutes(5)));

    var item = await Check("create category", HttpMethod.Post, "/api/danh-muc", 201, admin, new { tenDanhMuc = "  " + tag + "  ", moTa = "Kiểm thử", trangThai = false });
    var id = item.GetProperty("maDanhMuc").GetInt32();
    if (item.GetProperty("trangThai").GetBoolean() || item.GetProperty("tenDanhMuc").GetString() != tag)
        throw new Exception("Name trim / false status not persisted.");
    foreach (var (role, token) in tokens) await Check("detail " + role, HttpMethod.Get, $"/api/danh-muc/{id}", 200, token);
    await Check("duplicate name", HttpMethod.Post, "/api/danh-muc", 409, admin, new { tenDanhMuc = tag.ToUpperInvariant() });
    await Check("empty name", HttpMethod.Post, "/api/danh-muc", 400, admin, new { tenDanhMuc = "   " });
    await Check("null name", HttpMethod.Post, "/api/danh-muc", 400, admin, new { tenDanhMuc = (string?)null });
    await Check("long name", HttpMethod.Post, "/api/danh-muc", 400, admin, new { tenDanhMuc = new string('a', 101) });
    await Check("long description", HttpMethod.Post, "/api/danh-muc", 400, admin, new { tenDanhMuc = tag, moTa = new string('a', 501) });
    await Check("update category", HttpMethod.Put, $"/api/danh-muc/{id}", 200, admin, new { tenDanhMuc = tag + "-updated", trangThai = true });
    await Check("update invalid", HttpMethod.Put, $"/api/danh-muc/{id}", 400, admin, new { tenDanhMuc = " " });
    await Check("detail missing", HttpMethod.Get, "/api/danh-muc/0", 404, admin);
    await Check("update missing", HttpMethod.Put, "/api/danh-muc/0", 404, admin, new { tenDanhMuc = tag });
    await Check("delete missing", HttpMethod.Delete, "/api/danh-muc/0", 404, admin);
    var second = await Check("create second", HttpMethod.Post, "/api/danh-muc", 201, admin, new { tenDanhMuc = tag + "-second" });
    await Check("update duplicate", HttpMethod.Put, $"/api/danh-muc/{id}", 409, admin, new { tenDanhMuc = tag + "-second" });
    object Product(string sku, decimal price = 1000m, int? category = null) =>
        new { sku, tenSanPham = "  Sản phẩm thử  ", donViTinh = " Cái ", giaBan = price, maDanhMuc = category ?? id, trangThai = false };
    await Check("product anonymous", HttpMethod.Get, "/api/san-pham", 401);
    foreach (var (role, token) in tokens)
    {
        await Check("product list " + role, HttpMethod.Get, "/api/san-pham", 200, token);
        if (role == "Admin") continue;
        await Check("product create forbidden " + role, HttpMethod.Post, "/api/san-pham", 403, token, Product(tag));
        await Check("product update forbidden " + role, HttpMethod.Put, "/api/san-pham/0", 403, token, Product(tag));
        await Check("product delete forbidden " + role, HttpMethod.Delete, "/api/san-pham/0", 403, token);
    }
    var product = await Check("create product", HttpMethod.Post, "/api/san-pham", 201, admin, Product("  " + tag + "  "));
    var productId = product.GetProperty("maSanPham").GetInt32();
    if (product.GetProperty("sku").GetString() != tag || product.GetProperty("trangThai").GetBoolean()
        || product.GetProperty("tenSanPham").GetString() != "Sản phẩm thử" || product.GetProperty("donViTinh").GetString() != "Cái")
        throw new Exception("Product normalization/status failed.");
    foreach (var (role, token) in tokens)
    {
        await Check("product detail " + role, HttpMethod.Get, $"/api/san-pham/{productId}", 200, token);
        var stock = await Check("product stock " + role, HttpMethod.Get, $"/api/san-pham/{productId}/ton-kho", 200, token);
        if (stock.GetProperty("soLuongTon").GetInt64() != 0) throw new Exception("New product stock must be zero.");
    }
    await Check("duplicate SKU case and spaces", HttpMethod.Post, "/api/san-pham", 409, admin, Product(" " + tag.ToUpperInvariant() + " "));
    await Check("empty SKU", HttpMethod.Post, "/api/san-pham", 400, admin, Product(" "));
    await Check("long SKU", HttpMethod.Post, "/api/san-pham", 400, admin, Product(new string('a', 51)));
    await Check("missing category", HttpMethod.Post, "/api/san-pham", 400, admin, Product(tag, category: int.MaxValue));
    foreach (var price in new[] { -1m, 1.001m, 10000000000000000m })
    {
        await Check("invalid create price " + price, HttpMethod.Post, "/api/san-pham", 400, admin, Product(tag, price));
        await Check("invalid update price " + price, HttpMethod.Put, $"/api/san-pham/{productId}", 400, admin, Product(tag, price));
    }
    await Check("missing price", HttpMethod.Post, "/api/san-pham", 400, admin,
        new { sku = tag, tenSanPham = "Test", donViTinh = "Cái", maDanhMuc = id });
    foreach (var quantity in new object?[] { -1, 1.5, "abc", null, 0, 10 })
    {
        foreach (var field in new[] { "soLuong", "soLuongTon" })
        {
            var body = new Dictionary<string, object?> { ["sku"] = tag, ["tenSanPham"] = "Test", ["donViTinh"] = "Cái", ["maDanhMuc"] = id, ["giaBan"] = 1000, [field] = quantity };
            await Check($"create stock edit {field} {quantity}", HttpMethod.Post, "/api/san-pham", 400, admin, body);
            await Check($"update stock edit {field} {quantity}", HttpMethod.Put, $"/api/san-pham/{productId}", 400, admin, body);
        }
    }
    var unchanged = await Check("invalid updates do not persist", HttpMethod.Get, $"/api/san-pham/{productId}", 200, admin);
    if (unchanged.GetProperty("giaBan").GetDecimal() != 1000m) throw new Exception("Invalid price persisted.");
    await Check("update same SKU zero price", HttpMethod.Put, $"/api/san-pham/{productId}", 200, admin, Product(tag, 0));
    var otherProduct = await Check("second product max price", HttpMethod.Post, "/api/san-pham", 201, admin, Product(tag + "-p2", 9999999999999999.99m));
    await Check("update duplicate SKU", HttpMethod.Put, $"/api/san-pham/{productId}", 409, admin, Product(tag + "-p2"));
    await Check("product missing detail", HttpMethod.Get, "/api/san-pham/0", 404, admin);
    await Check("product missing stock", HttpMethod.Get, "/api/san-pham/0/ton-kho", 404, admin);
    await Check("product missing update", HttpMethod.Put, "/api/san-pham/0", 404, admin, Product(tag));
    await Check("product missing delete", HttpMethod.Delete, "/api/san-pham/0", 404, admin);
    await Check("category with API product", HttpMethod.Delete, $"/api/danh-muc/{id}", 409, admin);
    await Check("delete product", HttpMethod.Delete, $"/api/san-pham/{productId}", 200, admin);
    await Check("product deleted", HttpMethod.Get, $"/api/san-pham/{productId}", 404, admin);
    await Check("delete second product", HttpMethod.Delete, $"/api/san-pham/{otherProduct.GetProperty("maSanPham").GetInt32()}", 200, admin);
    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.SanPham(MaDanhMuc, SKU, TenSanPham, DonViTinh, GiaBan) VALUES ({id}, {tag}, N'Kiểm thử', N'Cái', 1000)");
    await Check("delete category in use", HttpMethod.Delete, $"/api/danh-muc/{id}", 409, admin);
    await db.SanPhams.Where(x => x.SKU.StartsWith(tag)).ExecuteDeleteAsync();
    await Check("delete category", HttpMethod.Delete, $"/api/danh-muc/{id}", 200, admin);
    await Check("deleted detail", HttpMethod.Get, $"/api/danh-muc/{id}", 404, admin);
    await Check("delete second", HttpMethod.Delete, $"/api/danh-muc/{second.GetProperty("maDanhMuc").GetInt32()}", 200, admin);

    var warehouseId = accounts["QuanLyKho"].MaNgDung;
    await db.NguoiDungs.Where(x => x.MaNgDung == warehouseId).ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, "Bị khóa"));
    await Check("locked existing session", HttpMethod.Get, "/api/auth/me", 401, tokens["QuanLyKho"]);
    await Check("locked login", HttpMethod.Post, "/api/auth/login", 401, body: new { email = accounts["QuanLyKho"].Email, password });
    await db.NguoiDungs.Where(x => x.MaNgDung == warehouseId).ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, "Hoạt động"));
    await db.NguoiDungs.Where(x => x.MaNgDung == warehouseId).ExecuteUpdateAsync(s => s.SetProperty(x => x.MaVaiTro, accounts["NhanVienBanHang"].MaVaiTro));
    await Check("changed role old token", HttpMethod.Get, "/api/auth/me", 401, tokens["QuanLyKho"]);
    await db.NguoiDungs.Where(x => x.MaNgDung == warehouseId).ExecuteUpdateAsync(s => s.SetProperty(x => x.MaVaiTro, accounts["QuanLyKho"].MaVaiTro));

    var otherSession = await Login("Admin");
    await Check("logout", HttpMethod.Post, "/api/auth/logout", 200, admin);
    await Check("revoked token me", HttpMethod.Get, "/api/auth/me", 401, admin);
    await Check("revoked token categories", HttpMethod.Get, "/api/danh-muc", 401, admin);
    await Check("repeated logout", HttpMethod.Post, "/api/auth/logout", 401, admin);
    await Check("other session survives", HttpMethod.Get, "/api/auth/me", 200, otherSession);
    await StopApi(); await StartApi();
    await Check("revocation survives restart", HttpMethod.Get, "/api/auth/me", 401, admin);
    await Check("active session survives restart", HttpMethod.Get, "/api/auth/me", 200, otherSession);

    using var swagger = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
    var paths = swagger.RootElement.GetProperty("paths");
    if (paths.GetProperty("/api/auth/login").GetProperty("post").TryGetProperty("security", out var loginSecurity) && loginSecurity.GetArrayLength() > 0)
        throw new Exception("Swagger login must be anonymous.");
    if (!paths.GetProperty("/api/danh-muc").GetProperty("get").TryGetProperty("security", out _))
        throw new Exception("Swagger categories must specify bearer security.");
    Console.WriteLine($"ALL {passed} HTTP CHECKS PASSED; Swagger security schema verified.");
    if (args.Contains("--swagger"))
    {
        Console.WriteLine($"SWAGGER {baseUrl}/swagger/index.html");
        foreach (var (role, user) in accounts) Console.WriteLine($"TEST USER {role}: {user.Email}");
        Console.WriteLine($"TEMPORARY TEST PASSWORD: {password}");
        Console.WriteLine("Create tests/swagger.stop to stop and clean up temporary accounts/categories.");
        while (!File.Exists(Path.Combine(root, "tests/swagger.stop"))) await Task.Delay(500);
    }
}
finally
{
    await StopApi();
    var ids = accounts.Values.Where(x => x.MaNgDung > 0).Select(x => x.MaNgDung).ToArray();
    await db.SanPhams.Where(x => x.SKU.StartsWith(tag)).ExecuteDeleteAsync();
    await db.DanhMucs.Where(x => x.TenDanhMuc.StartsWith(tag)).ExecuteDeleteAsync();
    await db.PhienDangNhaps.Where(x => ids.Contains(x.MaNgDung)).ExecuteDeleteAsync();
    await db.NguoiDungs.Where(x => ids.Contains(x.MaNgDung)).ExecuteDeleteAsync();
    Console.WriteLine("Temporary test data cleaned up.");
}
