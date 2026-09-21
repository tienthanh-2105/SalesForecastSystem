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
var accounts = new Dictionary<string, User>();
var managedUserIds = new HashSet<int>();
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
        throw new Exception($"FAIL {label}: expected {expected}, got {(int)response.StatusCode}: {content}\nAPI LOG:\n{string.Join('\n', apiLog)}");
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
    var roles = new[] { "Admin", "WarehouseManager", "SalesStaff" };
    foreach (var roleName in roles)
    {
        var roleId = await db.Roles.Where(role => role.Name == roleName).Select(role => role.RoleId).FirstAsync();
        var user = new User
        {
            RoleId = roleId,
            FullName = "Integration test " + roleName,
            Email = $"{tag}-{roleName}@example.invalid".ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 4),
            Status = UserStatuses.Active,
            CreatedAt = DateTime.UtcNow
        };
        accounts.Add(roleName, user);
        db.Users.Add(user);
    }
    await db.SaveChangesAsync();
    await StartApi();

    await Check("anonymous list", HttpMethod.Get, "/api/categories", 401);
    await Check("anonymous detail", HttpMethod.Get, "/api/categories/0", 401);
    await Check("anonymous create", HttpMethod.Post, "/api/categories", 401, body: new { name = tag });
    await Check("anonymous update", HttpMethod.Put, "/api/categories/0", 401, body: new { name = tag });
    await Check("anonymous delete", HttpMethod.Delete, "/api/categories/0", 401);
    await Check("anonymous logout", HttpMethod.Post, "/api/auth/logout", 401);
    await Check("invalid email", HttpMethod.Post, "/api/auth/login", 400, body: new { email = "bad", password });
    await Check("wrong password", HttpMethod.Post, "/api/auth/login", 401, body: new { email = accounts["Admin"].Email, password = "incorrect" });
    await Check("unknown email", HttpMethod.Post, "/api/auth/login", 401, body: new { email = $"{tag}@example.invalid", password });
    foreach (var role in accounts.Keys)
    {
        tokens[role] = await Login(role);
        var me = await Check("me " + role, HttpMethod.Get, "/api/auth/me", 200, tokens[role]);
        if (me.GetProperty("role").GetString() != role) throw new Exception("Role claim is incorrect.");
        await Check("list " + role, HttpMethod.Get, "/api/categories", 200, tokens[role]);
        if (role != "Admin")
        {
            await Check("create forbidden " + role, HttpMethod.Post, "/api/categories", 403, tokens[role], new { name = tag });
            await Check("update forbidden " + role, HttpMethod.Put, "/api/categories/0", 403, tokens[role], new { name = tag });
            await Check("delete forbidden " + role, HttpMethod.Delete, "/api/categories/0", 403, tokens[role]);
        }
    }
    foreach (var (role, token) in tokens)
    {
        foreach (var target in new[] { "Admin", "WarehouseManager", "SalesStaff" })
        {
            var route = target switch { "Admin" => "admin", "WarehouseManager" => "warehouse", _ => "sales" };
            await Check($"access {role} -> {target}", HttpMethod.Get, "/api/access-check/" + route,
                role == target ? 200 : 403, token);
        }
    }
    await Check("anonymous role endpoint", HttpMethod.Get, "/api/access-check/admin", 401);
    var admin = tokens["Admin"];
    var taxCode = tag[^16..];
    var warehouseManager = tokens["WarehouseManager"];
    var salesStaff = tokens["SalesStaff"];
    await Check("warehouses anonymous", HttpMethod.Get, "/api/warehouses", 401);
    await Check("suppliers anonymous", HttpMethod.Get, "/api/suppliers", 401);
    await Check("suppliers sales forbidden", HttpMethod.Get, "/api/suppliers", 403, salesStaff);
    await Check("warehouse sales create forbidden", HttpMethod.Post, "/api/warehouses", 403, salesStaff, new { name = tag });
    await Check("supplier sales create forbidden", HttpMethod.Post, "/api/suppliers", 403, salesStaff, new { name = tag });
    await Check("warehouse blank name", HttpMethod.Post, "/api/warehouses", 400, admin, new { name = "  " });
    await Check("supplier blank name", HttpMethod.Post, "/api/suppliers", 400, admin, new { name = "  " });
    await Check("supplier blank tax code", HttpMethod.Post, "/api/suppliers", 400, admin, new { name = tag, taxCode = "  " });
    await Check("supplier invalid email", HttpMethod.Post, "/api/suppliers", 400, admin, new { name = tag, email = "invalid" });
    await Check("supplier invalid phone", HttpMethod.Post, "/api/suppliers", 400, admin, new { name = tag, phoneNumber = "abc" });
    var warehouse = await Check("warehouse manager creates warehouse", HttpMethod.Post, "/api/warehouses", 201,
        warehouseManager, new { name = tag + "-warehouse", address = "Main depot", isActive = true });
    var warehouseId = warehouse.GetProperty("warehouseId").GetInt32();
    var supplier = await Check("warehouse manager creates supplier", HttpMethod.Post, "/api/suppliers", 201,
        warehouseManager, new { name = tag + "-supplier", taxCode, email = "TEST@EXAMPLE.INVALID", phoneNumber = "0901234567", isActive = true });
    var supplierId = supplier.GetProperty("supplierId").GetInt32();
    if (supplier.GetProperty("email").GetString() != "test@example.invalid") throw new Exception("Supplier email not normalized.");
    await Check("warehouse duplicate name", HttpMethod.Post, "/api/warehouses", 409, admin, new { name = tag + "-warehouse" });
    await Check("supplier duplicate tax code", HttpMethod.Post, "/api/suppliers", 409, admin, new { name = tag + "-other", taxCode });
    await Check("warehouse detail", HttpMethod.Get, $"/api/warehouses/{warehouseId}", 200, salesStaff);
    await Check("supplier detail", HttpMethod.Get, $"/api/suppliers/{supplierId}", 200, admin);
    var warehousePage = await Check("warehouse search and page", HttpMethod.Get,
        $"/api/warehouses?search={tag}&isActive=true&page=1&pageSize=1", 200, salesStaff);
    if (warehousePage.GetProperty("totalItems").GetInt32() != 1 || warehousePage.GetProperty("items").GetArrayLength() != 1)
        throw new Exception("Warehouse search or pagination failed.");
    var supplierPage = await Check("supplier search and page", HttpMethod.Get,
        $"/api/suppliers?search={tag}&isActive=true&page=1&pageSize=1", 200, warehouseManager);
    if (supplierPage.GetProperty("totalItems").GetInt32() != 1 || supplierPage.GetProperty("items").GetArrayLength() != 1)
        throw new Exception("Supplier search or pagination failed.");
    await Check("warehouse invalid page", HttpMethod.Get, "/api/warehouses?page=0", 400, admin);
    await Check("supplier invalid page size", HttpMethod.Get, "/api/suppliers?pageSize=101", 400, admin);
    await Check("warehouse missing", HttpMethod.Get, "/api/warehouses/0", 404, admin);
    await Check("supplier missing", HttpMethod.Get, "/api/suppliers/0", 404, admin);
    await Check("warehouse update", HttpMethod.Put, $"/api/warehouses/{warehouseId}", 200, admin,
        new { name = tag + "-warehouse", address = "Updated depot", isActive = true });
    await Check("supplier update", HttpMethod.Put, $"/api/suppliers/{supplierId}", 200, admin,
        new { name = tag + "-supplier", taxCode, email = "test@example.invalid", address = "Updated address", isActive = true });
    await Check("warehouse sales update forbidden", HttpMethod.Put, $"/api/warehouses/{warehouseId}", 403, salesStaff,
        new { name = tag + "-warehouse" });
    await Check("supplier sales delete forbidden", HttpMethod.Delete, $"/api/suppliers/{supplierId}", 403, salesStaff);
    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.PurchaseOrders(OrderNumber, WarehouseId, SupplierId, CreatedByUserId, OrderDate) VALUES ({"PO-" + tag}, {warehouseId}, {supplierId}, {accounts["Admin"].UserId}, {DateTime.UtcNow.Date})");
    await Check("referenced warehouse deactivates", HttpMethod.Delete, $"/api/warehouses/{warehouseId}", 200, warehouseManager);
    await Check("referenced supplier deactivates", HttpMethod.Delete, $"/api/suppliers/{supplierId}", 200, warehouseManager);
    var inactiveWarehouse = await Check("referenced warehouse retained", HttpMethod.Get, $"/api/warehouses/{warehouseId}", 200, admin);
    var inactiveSupplier = await Check("referenced supplier retained", HttpMethod.Get, $"/api/suppliers/{supplierId}", 200, admin);
    if (inactiveWarehouse.GetProperty("isActive").GetBoolean() || inactiveSupplier.GetProperty("isActive").GetBoolean())
        throw new Exception("Referenced master data was not deactivated.");
    await Check("warehouse inactive filter", HttpMethod.Get, $"/api/warehouses?search={tag}&isActive=false", 200, admin);
    await Check("supplier inactive filter", HttpMethod.Get, $"/api/suppliers?search={tag}&isActive=false", 200, admin);
    await Check("warehouse repeated deactivation", HttpMethod.Delete, $"/api/warehouses/{warehouseId}", 200, admin);
    await Check("supplier repeated deactivation", HttpMethod.Delete, $"/api/suppliers/{supplierId}", 200, admin);
    await Check("invalid token", HttpMethod.Get, "/api/categories", 401, "invalid");
    await Check("expired signed token", HttpMethod.Get, "/api/categories", 401, SignedToken(admin, key, "IntegrationChecks", DateTime.UtcNow.AddMinutes(-1)));
    await Check("wrong issuer", HttpMethod.Get, "/api/categories", 401, SignedToken(admin, key, "Other", DateTime.UtcNow.AddMinutes(5)));
    await Check("wrong signature", HttpMethod.Get, "/api/categories", 401, SignedToken(admin, new string('x', 48), "IntegrationChecks", DateTime.UtcNow.AddMinutes(5)));

    await Check("users anonymous", HttpMethod.Get, "/api/users", 401);
    await Check("users warehouse forbidden", HttpMethod.Get, "/api/users", 403, tokens["WarehouseManager"]);
    await Check("users sales forbidden", HttpMethod.Get, "/api/users", 403, tokens["SalesStaff"]);
    await Check("users create non-admin forbidden", HttpMethod.Post, "/api/users", 403, tokens["WarehouseManager"], new
    {
        fullName = "Forbidden user", email = $"{tag}-forbidden@example.invalid", phoneNumber = "0123456789",
        role = "SalesStaff", password
    });

    object UserWrite(string email, string role = "SalesStaff", string fullName = "  Managed user  ") => new
    {
        fullName,
        email,
        phoneNumber = " 0123456789 ",
        role
    };

    var managedEmail = $"{tag}-managed@example.invalid";
    var createUserBody = new
    {
        fullName = "  Managed user  ",
        email = "  " + managedEmail.ToUpperInvariant() + "  ",
        phoneNumber = " 0123456789 ",
        role = "salesstaff",
        password
    };
    var managedUser = await Check("admin creates user", HttpMethod.Post, "/api/users", 201, admin, createUserBody);
    var managedUserId = managedUser.GetProperty("userId").GetInt32();
    managedUserIds.Add(managedUserId);
    if (managedUser.GetProperty("fullName").GetString() != "Managed user"
        || managedUser.GetProperty("email").GetString() != managedEmail
        || managedUser.GetProperty("phoneNumber").GetString() != "0123456789"
        || managedUser.GetProperty("roleName").GetString() != "SalesStaff"
        || managedUser.GetProperty("status").GetString() != "Active"
        || managedUser.GetRawText().Contains("password", StringComparison.OrdinalIgnoreCase))
        throw new Exception("Created user was not normalized or exposed password data.");

    await Check("duplicate user email", HttpMethod.Post, "/api/users", 409, admin, createUserBody);
    await Check("invalid user email", HttpMethod.Post, "/api/users", 400, admin,
        new { fullName = "Test", email = "invalid", role = "SalesStaff", password });
    await Check("invalid user role", HttpMethod.Post, "/api/users", 400, admin,
        new { fullName = "Test", email = $"{tag}-invalid-role@example.invalid", role = "Unknown", password });
    await Check("empty user name", HttpMethod.Post, "/api/users", 400, admin,
        new { fullName = " ", email = $"{tag}-empty-name@example.invalid", role = "SalesStaff", password });
    await Check("short user password", HttpMethod.Post, "/api/users", 400, admin,
        new { fullName = "Test", email = $"{tag}-short-password@example.invalid", role = "SalesStaff", password = "short" });
    await Check("long UTF8 user password", HttpMethod.Post, "/api/users", 400, admin,
        new { fullName = "Test", email = $"{tag}-long-password@example.invalid", role = "SalesStaff", password = new string('ắ', 40) });
    await Check("invalid user phone", HttpMethod.Post, "/api/users", 400, admin,
        new { fullName = "Test", email = $"{tag}-invalid-phone@example.invalid", phoneNumber = "not-a-phone", role = "SalesStaff", password });

    var managedDetail = await Check("admin gets user detail", HttpMethod.Get, $"/api/users/{managedUserId}", 200, admin);
    if (managedDetail.GetRawText().Contains("password", StringComparison.OrdinalIgnoreCase))
        throw new Exception("User detail exposed password data.");
    await Check("missing user detail", HttpMethod.Get, "/api/users/0", 404, admin);

    var managedToken = (await Check("managed user login", HttpMethod.Post, "/api/auth/login", 200,
        body: new { email = managedEmail, password })).GetProperty("accessToken").GetString()!;
    var updatedManagedUser = await Check("admin updates user role", HttpMethod.Put, $"/api/users/{managedUserId}", 200, admin,
        UserWrite(managedEmail, "WarehouseManager", " Updated managed user "));
    if (updatedManagedUser.GetProperty("roleName").GetString() != "WarehouseManager"
        || updatedManagedUser.GetProperty("fullName").GetString() != "Updated managed user")
        throw new Exception("User update was not persisted.");
    await Check("changed user role invalidates token", HttpMethod.Get, "/api/auth/me", 401, managedToken);
    await Check("duplicate email on user update", HttpMethod.Put, $"/api/users/{managedUserId}", 409, admin,
        UserWrite(accounts["SalesStaff"].Email, "WarehouseManager"));
    await Check("missing user update", HttpMethod.Put, "/api/users/0", 404, admin,
        UserWrite($"{tag}-missing@example.invalid"));

    var userList = await Check("user search role status pagination", HttpMethod.Get,
        $"/api/users?search={Uri.EscapeDataString(tag + "-managed")}&role=warehouseManager&status=active&page=1&pageSize=1", 200, admin);
    if (userList.GetProperty("items").GetArrayLength() != 1
        || userList.GetProperty("pageSize").GetInt32() != 1
        || userList.GetProperty("totalItems").GetInt32() != 1
        || userList.GetProperty("items")[0].GetProperty("userId").GetInt32() != managedUserId
        || userList.GetRawText().Contains("password", StringComparison.OrdinalIgnoreCase))
        throw new Exception("User search/filter/pagination is incorrect or exposed password data.");
    var emptyUserPage = await Check("user page beyond total", HttpMethod.Get,
        $"/api/users?search={Uri.EscapeDataString(tag + "-managed")}&page=2147483647&pageSize=100", 200, admin);
    if (emptyUserPage.GetProperty("items").GetArrayLength() != 0)
        throw new Exception("User page beyond the result set must be empty.");
    await Check("invalid user page", HttpMethod.Get, "/api/users?page=0", 400, admin);
    await Check("invalid user page size", HttpMethod.Get, "/api/users?pageSize=101", 400, admin);
    await Check("invalid user role filter", HttpMethod.Get, "/api/users?role=Unknown", 400, admin);
    await Check("invalid user status filter", HttpMethod.Get, "/api/users?status=Unknown", 400, admin);
    await Check("long user search", HttpMethod.Get, "/api/users?search=" + new string('a', 201), 400, admin);

    managedToken = (await Check("updated user login", HttpMethod.Post, "/api/auth/login", 200,
        body: new { email = managedEmail, password })).GetProperty("accessToken").GetString()!;
    var lockedUser = await Check("admin locks user", HttpMethod.Put, $"/api/users/{managedUserId}/status", 200, admin,
        new { status = "locked" });
    if (lockedUser.GetProperty("status").GetString() != "Locked")
        throw new Exception("User was not locked.");
    await Check("locked user old token", HttpMethod.Get, "/api/auth/me", 401, managedToken);
    await Check("locked user login", HttpMethod.Post, "/api/auth/login", 401,
        body: new { email = managedEmail, password });
    await Check("admin unlocks user", HttpMethod.Put, $"/api/users/{managedUserId}/status", 200, admin,
        new { status = "Active" });
    managedToken = (await Check("unlocked user login", HttpMethod.Post, "/api/auth/login", 200,
        body: new { email = managedEmail, password })).GetProperty("accessToken").GetString()!;

    var newPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(25));
    await Check("admin resets password", HttpMethod.Post, $"/api/users/{managedUserId}/reset-password", 200, admin,
        new { newPassword });
    await Check("password reset revokes token", HttpMethod.Get, "/api/auth/me", 401, managedToken);
    await Check("old password rejected", HttpMethod.Post, "/api/auth/login", 401,
        body: new { email = managedEmail, password });
    await Check("new password accepted", HttpMethod.Post, "/api/auth/login", 200,
        body: new { email = managedEmail, password = newPassword });
    await Check("short reset password", HttpMethod.Post, $"/api/users/{managedUserId}/reset-password", 400, admin,
        new { newPassword = "short" });
    await Check("missing password reset user", HttpMethod.Post, "/api/users/0/reset-password", 404, admin,
        new { newPassword });
    await Check("invalid status", HttpMethod.Put, $"/api/users/{managedUserId}/status", 400, admin,
        new { status = "Unknown" });
    await Check("missing status user", HttpMethod.Put, "/api/users/0/status", 404, admin,
        new { status = "Locked" });
    await Check("admin cannot lock self", HttpMethod.Put, $"/api/users/{accounts["Admin"].UserId}/status", 400, admin,
        new { status = "Locked" });
    await Check("admin cannot change own role", HttpMethod.Put, $"/api/users/{accounts["Admin"].UserId}", 400, admin,
        UserWrite(accounts["Admin"].Email, "SalesStaff", accounts["Admin"].FullName));
    if (apiLog.Any(line => line.Contains(password, StringComparison.Ordinal)
                           || line.Contains(newPassword, StringComparison.Ordinal)))
        throw new Exception("API logs exposed a test password.");

    var item = await Check("create category", HttpMethod.Post, "/api/categories", 201, admin, new { name = "  " + tag + "  ", description = "Integration test", isActive = false });
    var id = item.GetProperty("categoryId").GetInt32();
    if (item.GetProperty("isActive").GetBoolean() || item.GetProperty("name").GetString() != tag)
        throw new Exception("Name trim / false status not persisted.");
    foreach (var (role, token) in tokens) await Check("detail " + role, HttpMethod.Get, $"/api/categories/{id}", 200, token);
    await Check("duplicate name", HttpMethod.Post, "/api/categories", 409, admin, new { name = tag.ToUpperInvariant() });
    await Check("empty name", HttpMethod.Post, "/api/categories", 400, admin, new { name = "   " });
    await Check("null name", HttpMethod.Post, "/api/categories", 400, admin, new { name = (string?)null });
    await Check("long name", HttpMethod.Post, "/api/categories", 400, admin, new { name = new string('a', 101) });
    await Check("long description", HttpMethod.Post, "/api/categories", 400, admin, new { name = tag, description = new string('a', 501) });
    await Check("update category", HttpMethod.Put, $"/api/categories/{id}", 200, admin, new { name = tag + "-updated", isActive = true });
    await Check("update invalid", HttpMethod.Put, $"/api/categories/{id}", 400, admin, new { name = " " });
    await Check("detail missing", HttpMethod.Get, "/api/categories/0", 404, admin);
    await Check("update missing", HttpMethod.Put, "/api/categories/0", 404, admin, new { name = tag });
    await Check("delete missing", HttpMethod.Delete, "/api/categories/0", 404, admin);
    var second = await Check("create second", HttpMethod.Post, "/api/categories", 201, admin, new { name = tag + "-second" });
    await Check("update duplicate", HttpMethod.Put, $"/api/categories/{id}", 409, admin, new { name = tag + "-second" });
    object Product(
        string sku,
        decimal price = 1000m,
        int? category = null,
        string? rowVersion = null,
        int minimumStockLevel = 5,
        bool isActive = false)
    {
        var body = new Dictionary<string, object?>
        {
            ["sku"] = sku,
            ["name"] = "  Test product  ",
            ["unit"] = " Item ",
            ["description"] = "  Integration product  ",
            ["imageUrl"] = "https://example.invalid/product.png",
            ["salePrice"] = price,
            ["minimumStockLevel"] = minimumStockLevel,
            ["categoryId"] = category ?? id,
            ["isActive"] = isActive
        };
        if (rowVersion is not null) body["rowVersion"] = rowVersion;
        return body;
    }
    await Check("product anonymous", HttpMethod.Get, "/api/products", 401);
    foreach (var (role, token) in tokens)
    {
        await Check("product list " + role, HttpMethod.Get, "/api/products", 200, token);
        if (role != "SalesStaff") continue;
        await Check("product create forbidden " + role, HttpMethod.Post, "/api/products", 403, token, Product(tag));
        await Check("product update forbidden " + role, HttpMethod.Put, "/api/products/0", 403, token, Product(tag, rowVersion: Convert.ToBase64String(new byte[8])));
        await Check("product delete forbidden " + role, HttpMethod.Delete, "/api/products/0", 403, token);
    }
    var warehouseToken = tokens["WarehouseManager"];
    var warehouseProduct = await Check("warehouse manager creates product", HttpMethod.Post, "/api/products", 201, warehouseToken, Product(tag + "-warehouse", isActive: true));
    var warehouseProductId = warehouseProduct.GetProperty("productId").GetInt32();
    var warehouseUpdate = await Check("warehouse manager updates product", HttpMethod.Put, $"/api/products/{warehouseProductId}", 200, warehouseToken,
        Product(tag + "-warehouse", rowVersion: warehouseProduct.GetProperty("rowVersion").GetString(), minimumStockLevel: 12, isActive: true));
    if (warehouseUpdate.GetProperty("minimumStockLevel").GetInt32() != 12)
        throw new Exception("Warehouse manager product update was not persisted.");
    await Check("warehouse manager deactivates product", HttpMethod.Delete, $"/api/products/{warehouseProductId}", 200, warehouseToken);
    var inactiveWarehouseProduct = await Check("deactivated warehouse product retained", HttpMethod.Get, $"/api/products/{warehouseProductId}", 200, warehouseToken);
    if (inactiveWarehouseProduct.GetProperty("isActive").GetBoolean())
        throw new Exception("Deactivated product is still active.");
    var product = await Check("create product", HttpMethod.Post, "/api/products", 201, admin, Product("  " + tag + "  "));
    var productId = product.GetProperty("productId").GetInt32();
    var productRowVersion = product.GetProperty("rowVersion").GetString()!;
    if (product.GetProperty("sku").GetString() != tag || product.GetProperty("isActive").GetBoolean()
        || product.GetProperty("name").GetString() != "Test product" || product.GetProperty("unit").GetString() != "Item"
        || product.GetProperty("description").GetString() != "Integration product"
        || product.GetProperty("minimumStockLevel").GetInt32() != 5
        || string.IsNullOrWhiteSpace(productRowVersion))
        throw new Exception("Product normalization/status failed.");
    foreach (var (role, token) in tokens)
    {
        await Check("product detail " + role, HttpMethod.Get, $"/api/products/{productId}", 200, token);
        var stock = await Check("product stock " + role, HttpMethod.Get, $"/api/products/{productId}/stock", 200, token);
        if (stock.GetProperty("quantityOnHand").GetInt64() != 0) throw new Exception("New product stock must be zero.");
    }
    await Check("duplicate SKU case and spaces", HttpMethod.Post, "/api/products", 409, admin, Product(" " + tag.ToUpperInvariant() + " "));
    await Check("empty SKU", HttpMethod.Post, "/api/products", 400, admin, Product(" "));
    await Check("long SKU", HttpMethod.Post, "/api/products", 400, admin, Product(new string('a', 51)));
    await Check("missing category", HttpMethod.Post, "/api/products", 400, admin, Product(tag, category: int.MaxValue));
    await Check("negative minimum stock", HttpMethod.Post, "/api/products", 400, admin, Product(tag, minimumStockLevel: -1));
    await Check("invalid image URL", HttpMethod.Post, "/api/products", 400, admin,
        new { sku = tag, name = "Test", unit = "Item", description = "Test", imageUrl = "not-a-url", categoryId = id, salePrice = 1000, minimumStockLevel = 0 });
    await Check("long description", HttpMethod.Post, "/api/products", 400, admin,
        new { sku = tag, name = "Test", unit = "Item", description = new string('a', 1001), categoryId = id, salePrice = 1000, minimumStockLevel = 0 });
    foreach (var price in new[] { -1m, 1.001m, 10000000000000000m })
    {
        await Check("invalid create price " + price, HttpMethod.Post, "/api/products", 400, admin, Product(tag, price));
        await Check("invalid update price " + price, HttpMethod.Put, $"/api/products/{productId}", 400, admin, Product(tag, price, rowVersion: productRowVersion));
    }
    await Check("missing price", HttpMethod.Post, "/api/products", 400, admin,
        new { sku = tag, name = "Test", unit = "Item", categoryId = id });
    foreach (var quantity in new object?[] { -1, 1.5, "abc", null, 0, 10 })
    {
        foreach (var field in new[] { "quantity", "quantityOnHand" })
        {
            var body = new Dictionary<string, object?> { ["sku"] = tag, ["name"] = "Test", ["unit"] = "Item", ["categoryId"] = id, ["salePrice"] = 1000, ["rowVersion"] = productRowVersion, [field] = quantity };
            await Check($"create stock edit {field} {quantity}", HttpMethod.Post, "/api/products", 400, admin, body);
            await Check($"update stock edit {field} {quantity}", HttpMethod.Put, $"/api/products/{productId}", 400, admin, body);
        }
    }
    var unchanged = await Check("invalid updates do not persist", HttpMethod.Get, $"/api/products/{productId}", 200, admin);
    if (unchanged.GetProperty("salePrice").GetDecimal() != 1000m) throw new Exception("Invalid price persisted.");
    await Check("missing row version", HttpMethod.Put, $"/api/products/{productId}", 400, admin, Product(tag));
    await Check("invalid row version", HttpMethod.Put, $"/api/products/{productId}", 400, admin, Product(tag, rowVersion: "not-base64"));
    await Check("negative minimum stock update", HttpMethod.Put, $"/api/products/{productId}", 400, admin,
        Product(tag, rowVersion: productRowVersion, minimumStockLevel: -1));
    var updatedProduct = await Check("update same SKU zero price", HttpMethod.Put, $"/api/products/{productId}", 200, admin, Product(tag, 0, rowVersion: productRowVersion));
    var currentRowVersion = updatedProduct.GetProperty("rowVersion").GetString()!;
    if (currentRowVersion == productRowVersion) throw new Exception("Row version did not change after update.");
    await Check("stale row version conflict", HttpMethod.Put, $"/api/products/{productId}", 409, admin, Product(tag, 1, rowVersion: productRowVersion));
    var otherProduct = await Check("second product max price", HttpMethod.Post, "/api/products", 201, admin,
        Product(tag + "-p2", 9999999999999999.99m, isActive: true));
    var filteredProducts = await Check("product search filter sort pagination", HttpMethod.Get,
        $"/api/products?search={Uri.EscapeDataString(tag)}&categoryId={id}&sortBy=sku&sortDirection=desc&page=1&pageSize=2",
        200, admin);
    if (filteredProducts.GetProperty("page").GetInt32() != 1
        || filteredProducts.GetProperty("pageSize").GetInt32() != 2
        || filteredProducts.GetProperty("totalItems").GetInt32() < 2
        || filteredProducts.GetProperty("items").GetArrayLength() != 2)
        throw new Exception("Product pagination metadata is incorrect.");
    var sortedItems = filteredProducts.GetProperty("items").EnumerateArray().ToArray();
    if (string.Compare(sortedItems[0].GetProperty("sku").GetString(), sortedItems[1].GetProperty("sku").GetString(), StringComparison.OrdinalIgnoreCase) < 0)
        throw new Exception("Product SKU descending sort is incorrect.");
    if (sortedItems.Any(item => item.GetProperty("stockStatus").GetString() != "OutOfStock"
                                || item.GetProperty("quantityOnHand").GetInt64() != 0))
        throw new Exception("New products must be classified as out of stock.");

    var activeProducts = await Check("product active filter", HttpMethod.Get,
        $"/api/products?search={Uri.EscapeDataString(tag)}&isActive=true", 200, admin);
    if (activeProducts.GetProperty("items").EnumerateArray().Any(item => !item.GetProperty("isActive").GetBoolean()))
        throw new Exception("Active product filter returned an inactive product.");

    var outOfStockProducts = await Check("product stock filter", HttpMethod.Get,
        $"/api/products?search={Uri.EscapeDataString(tag)}&stockStatus=OutOfStock", 200, admin);
    if (outOfStockProducts.GetProperty("items").EnumerateArray().Any(item => item.GetProperty("stockStatus").GetString() != "OutOfStock"))
        throw new Exception("Out-of-stock filter returned another stock status.");
    foreach (var stockStatus in new[] { "Low", "Available" })
    {
        var stockProducts = await Check("product stock filter " + stockStatus, HttpMethod.Get,
            "/api/products?stockStatus=" + stockStatus, 200, admin);
        if (stockProducts.GetProperty("items").EnumerateArray().Any(item => item.GetProperty("stockStatus").GetString() != stockStatus))
            throw new Exception($"{stockStatus} filter returned another stock status.");
    }

    var emptyProductPage = await Check("product page beyond total", HttpMethod.Get,
        $"/api/products?search={Uri.EscapeDataString(tag)}&page=999&pageSize=20", 200, admin);
    if (emptyProductPage.GetProperty("items").GetArrayLength() != 0)
        throw new Exception("Page beyond the result set must be empty.");

    await Check("product invalid page", HttpMethod.Get, "/api/products?page=0", 400, admin);
    await Check("product invalid page size zero", HttpMethod.Get, "/api/products?pageSize=0", 400, admin);
    await Check("product invalid page size maximum", HttpMethod.Get, "/api/products?pageSize=101", 400, admin);
    await Check("product invalid category filter", HttpMethod.Get, "/api/products?categoryId=0", 400, admin);
    await Check("product invalid stock filter", HttpMethod.Get, "/api/products?stockStatus=Unknown", 400, admin);
    await Check("product invalid numeric stock filter", HttpMethod.Get, "/api/products?stockStatus=99", 400, admin);
    await Check("product invalid sort field", HttpMethod.Get, "/api/products?sortBy=Unknown", 400, admin);
    await Check("product invalid numeric sort field", HttpMethod.Get, "/api/products?sortBy=99", 400, admin);
    await Check("product invalid sort direction", HttpMethod.Get, "/api/products?sortDirection=sideways", 400, admin);
    await Check("product invalid numeric sort direction", HttpMethod.Get, "/api/products?sortDirection=99", 400, admin);
    await Check("product maximum page does not overflow", HttpMethod.Get, "/api/products?page=2147483647&pageSize=100", 200, admin);
    await Check("product long search", HttpMethod.Get, "/api/products?search=" + new string('a', 201), 400, admin);
    await Check("update duplicate SKU", HttpMethod.Put, $"/api/products/{productId}", 409, admin, Product(tag + "-p2", rowVersion: currentRowVersion));
    await Check("product missing detail", HttpMethod.Get, "/api/products/0", 404, admin);
    await Check("product missing stock", HttpMethod.Get, "/api/products/0/stock", 404, admin);
    await Check("product missing update", HttpMethod.Put, "/api/products/0", 404, admin, Product(tag, rowVersion: currentRowVersion));
    await Check("product missing delete", HttpMethod.Delete, "/api/products/0", 404, admin);
    await Check("category with API product", HttpMethod.Delete, $"/api/categories/{id}", 409, admin);
    await Check("deactivate product", HttpMethod.Delete, $"/api/products/{productId}", 200, admin);
    var deactivatedProduct = await Check("deactivated product retained", HttpMethod.Get, $"/api/products/{productId}", 200, admin);
    if (deactivatedProduct.GetProperty("isActive").GetBoolean()) throw new Exception("Product was not deactivated.");
    if (!await db.Products.AnyAsync(x => x.ProductId == productId)) throw new Exception("Product was physically deleted.");
    await Check("deactivate product idempotent", HttpMethod.Delete, $"/api/products/{productId}", 200, admin);
    await Check("deactivate second product", HttpMethod.Delete, $"/api/products/{otherProduct.GetProperty("productId").GetInt32()}", 200, admin);
    await db.Products.Where(x => x.SKU.StartsWith(tag)).ExecuteDeleteAsync();
    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.Products(CategoryId, SKU, Name, Unit, SalePrice) VALUES ({id}, {tag}, N'Integration test', N'Item', 1000)");
    await Check("delete category in use", HttpMethod.Delete, $"/api/categories/{id}", 409, admin);
    await db.Products.Where(x => x.SKU.StartsWith(tag)).ExecuteDeleteAsync();
    await Check("delete category", HttpMethod.Delete, $"/api/categories/{id}", 200, admin);
    await Check("deleted detail", HttpMethod.Get, $"/api/categories/{id}", 404, admin);
    await Check("delete second", HttpMethod.Delete, $"/api/categories/{second.GetProperty("categoryId").GetInt32()}", 200, admin);

    var warehouseManagerId = accounts["WarehouseManager"].UserId;
    await db.Users.Where(x => x.UserId == warehouseManagerId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, UserStatuses.Locked));
    await Check("locked existing session", HttpMethod.Get, "/api/auth/me", 401, tokens["WarehouseManager"]);
    await Check("locked login", HttpMethod.Post, "/api/auth/login", 401, body: new { email = accounts["WarehouseManager"].Email, password });
    await db.Users.Where(x => x.UserId == warehouseManagerId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, UserStatuses.Active));
    await db.Users.Where(x => x.UserId == warehouseManagerId).ExecuteUpdateAsync(s => s.SetProperty(x => x.RoleId, accounts["SalesStaff"].RoleId));
    await Check("changed role old token", HttpMethod.Get, "/api/auth/me", 401, tokens["WarehouseManager"]);
    await db.Users.Where(x => x.UserId == warehouseManagerId).ExecuteUpdateAsync(s => s.SetProperty(x => x.RoleId, accounts["WarehouseManager"].RoleId));

    var otherSession = await Login("Admin");
    await Check("logout", HttpMethod.Post, "/api/auth/logout", 200, admin);
    await Check("revoked token me", HttpMethod.Get, "/api/auth/me", 401, admin);
    await Check("revoked token categories", HttpMethod.Get, "/api/categories", 401, admin);
    await Check("repeated logout", HttpMethod.Post, "/api/auth/logout", 401, admin);
    await Check("other session survives", HttpMethod.Get, "/api/auth/me", 200, otherSession);
    await StopApi(); await StartApi();
    await Check("revocation survives restart", HttpMethod.Get, "/api/auth/me", 401, admin);
    await Check("active session survives restart", HttpMethod.Get, "/api/auth/me", 200, otherSession);

    using var swagger = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
    var paths = swagger.RootElement.GetProperty("paths");
    if (paths.GetProperty("/api/auth/login").GetProperty("post").TryGetProperty("security", out var loginSecurity) && loginSecurity.GetArrayLength() > 0)
        throw new Exception("Swagger login must be anonymous.");
    if (!paths.GetProperty("/api/categories").GetProperty("get").TryGetProperty("security", out _))
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
    var ids = accounts.Values.Where(x => x.UserId > 0).Select(x => x.UserId).Concat(managedUserIds).ToArray();
    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.PurchaseOrders WHERE OrderNumber = {"PO-" + tag}");
    await db.Warehouses.Where(x => x.Name.StartsWith(tag)).ExecuteDeleteAsync();
    await db.Suppliers.Where(x => x.Name.StartsWith(tag)).ExecuteDeleteAsync();
    await db.Products.Where(x => x.SKU.StartsWith(tag)).ExecuteDeleteAsync();
    await db.Categories.Where(x => x.Name.StartsWith(tag)).ExecuteDeleteAsync();
    await db.LoginSessions.Where(x => ids.Contains(x.UserId)).ExecuteDeleteAsync();
    await db.Users.Where(x => ids.Contains(x.UserId)).ExecuteDeleteAsync();
    Console.WriteLine("Temporary test data cleaned up.");
}
