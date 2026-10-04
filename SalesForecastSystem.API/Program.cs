using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SalesForecastSystem.API.Services;
using SalesForecastSystem.API.Swagger;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Seeders;
using SalesForecastSystem.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IInventoryTransactionService, InventoryTransactionService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IForecastDataService, ForecastDataService>();
builder.Services.AddScoped<SessionJwtEvents>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer",
        BearerFormat = "JWT", In = ParameterLocation.Header,
        Description = "Dán accessToken vào đây (không thêm tiền tố Bearer)."
    });
    options.OperationFilter<AuthorizeOperationFilter>();
});

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Chưa cấu hình Jwt:Key.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Chưa cấu hình Jwt:Issuer.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Chưa cấu hình Jwt:Audience.");
if (jwtKey == "THAY_BANG_KHOA_NGAU_NHIEN" || Encoding.UTF8.GetByteCount(jwtKey) < 32
    || string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException("Cần khóa JWT ngẫu nhiên ít nhất 32 byte, Issuer và Audience hợp lệ.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.EventsType = typeof(SessionJwtEvents);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer, ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            NameClaimType = "name", RoleClaimType = "role", ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    var adminEmail = app.Configuration["SeedAdmin:Email"];
    var adminPassword = app.Configuration["SeedAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        using var scope = app.Services.CreateScope();
        await DataSeeder.SeedAdminAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), adminEmail, adminPassword);
    }
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        if (!context.File.Name.Equals("index.html", StringComparison.OrdinalIgnoreCase))
        {
            if (context.Context.Request.Path.StartsWithSegments("/assets"))
                context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return;
        }
        context.Context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        context.Context.Response.Headers.Pragma = "no-cache";
        context.Context.Response.Headers.Expires = "0";
    }
});
app.Use(async (context, next) =>
{
    // Public static resources are served above; missing resources must stay 404.
    if (context.GetEndpoint() is null &&
        (context.Request.Path.StartsWithSegments("/assets") ||
         context.Request.Path.StartsWithSegments("/uploads") ||
         Path.HasExtension(context.Request.Path.Value)))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next(context);
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Only known UI routes may use the SPA shell. API/assets/uploads never fall back to HTML.
foreach (var route in new[] { "/", "/login", "/categories", "/products", "/warehouses", "/purchases", "/orders", "/customers", "/users", "/forbidden" })
{
    app.MapGet(route, async (HttpContext context, IWebHostEnvironment environment) =>
    {
        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(Path.Combine(environment.WebRootPath, "index.html"));
    }).AllowAnonymous();
}
app.MapFallback(async (HttpContext context, IWebHostEnvironment environment) =>
{
    var path = context.Request.Path;
    context.Response.StatusCode = StatusCodes.Status404NotFound;
    if (!HttpMethods.IsGet(context.Request.Method) ||
        path.StartsWithSegments("/api") || path.StartsWithSegments("/swagger") ||
        path.StartsWithSegments("/uploads") || path.StartsWithSegments("/assets") ||
        Path.HasExtension(path.Value)) return;
    context.Response.ContentType = "text/html; charset=utf-8";
    context.Response.Headers.CacheControl = "no-store";
    await context.Response.SendFileAsync(Path.Combine(environment.WebRootPath, "index.html"));
}).AllowAnonymous();
app.Run();
