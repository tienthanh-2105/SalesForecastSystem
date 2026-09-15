using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.DTOs.SanPham;
using SalesForecastSystem.Core.Helpers;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.API.Controllers;

[ApiController]
[Route("api/san-pham")]
[Authorize(Roles = RoleNames.All)]
public class SanPhamController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<SanPhamResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await context.SanPhams.AsNoTracking().OrderBy(x => x.MaSanPham)
            .Select(x => new SanPhamResponse(x.MaSanPham, x.MaDanhMuc, x.SKU, x.TenSanPham,
                x.DonViTinh, x.GiaBan, x.TrangThai, x.NgayTao)).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SanPhamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var item = await context.SanPhams.AsNoTracking().SingleOrDefaultAsync(x => x.MaSanPham == id, cancellationToken);
        return item is null ? Missing() : Ok(ToResponse(item));
    }

    [HttpGet("{id:int}/ton-kho")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Stock(int id, CancellationToken cancellationToken)
    {
        if (!await context.SanPhams.AnyAsync(x => x.MaSanPham == id, cancellationToken)) return Missing();
        var quantity = await context.Database.SqlQuery<long>($"SELECT COALESCE(SUM(CONVERT(bigint, SoLuong)), CONVERT(bigint, 0)) AS [Value] FROM dbo.GiaoDichKho WHERE MaSanPham = {id}")
            .SingleAsync(cancellationToken);
        return Ok(new { maSanPham = id, soLuongTon = quantity });
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(SanPhamResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(SanPhamRequest request, CancellationToken cancellationToken)
    {
        var error = await ValidateReferences(request, null, cancellationToken);
        if (error is not null) return error;
        var item = new SanPham();
        Apply(item, request);
        context.SanPhams.Add(item);
        var saveError = await Save(cancellationToken);
        return saveError ?? CreatedAtAction(nameof(Detail), new { id = item.MaSanPham }, ToResponse(item));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(SanPhamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, SanPhamRequest request, CancellationToken cancellationToken)
    {
        var item = await context.SanPhams.SingleOrDefaultAsync(x => x.MaSanPham == id, cancellationToken);
        if (item is null) return Missing();
        var error = await ValidateReferences(request, id, cancellationToken);
        if (error is not null) return error;
        Apply(item, request);
        var saveError = await Save(cancellationToken);
        return saveError ?? Ok(ToResponse(item));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var item = await context.SanPhams.SingleOrDefaultAsync(x => x.MaSanPham == id, cancellationToken);
        if (item is null) return Missing();
        context.SanPhams.Remove(item);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return ConcurrentChange(); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            return Problem(statusCode: 409, title: "Sản phẩm đã được sử dụng, không thể xóa. Có thể tắt trạng thái sản phẩm.");
        }
        return Ok(new { message = "Xóa sản phẩm thành công." });
    }

    private async Task<IActionResult?> ValidateReferences(SanPhamRequest request, int? id, CancellationToken cancellationToken)
    {
        if (!await context.DanhMucs.AnyAsync(x => x.MaDanhMuc == request.MaDanhMuc, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.MaDanhMuc), "Danh mục không tồn tại.");
            return ValidationProblem(ModelState);
        }
        var sku = request.SKU.Trim();
        if (await context.SanPhams.AnyAsync(x => x.SKU == sku && (!id.HasValue || x.MaSanPham != id.Value), cancellationToken))
            return Duplicate();
        return null;
    }

    private async Task<IActionResult?> Save(CancellationToken cancellationToken)
    {
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return ConcurrentChange(); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 }) { return Duplicate(); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            return Problem(statusCode: 409, title: "Dữ liệu liên quan đã thay đổi hoặc vi phạm ràng buộc. Vui lòng tải lại và thử lại.");
        }
        return null;
    }

    private static void Apply(SanPham item, SanPhamRequest request)
    {
        item.MaDanhMuc = request.MaDanhMuc!.Value;
        item.SKU = request.SKU.Trim();
        item.TenSanPham = request.TenSanPham.Trim();
        item.DonViTinh = request.DonViTinh.Trim();
        item.GiaBan = request.GiaBan!.Value;
        item.TrangThai = request.TrangThai;
    }

    private ObjectResult Missing() => Problem(statusCode: 404, title: "Không tìm thấy sản phẩm.");
    private ObjectResult Duplicate() => Problem(statusCode: 409, title: "Mã SKU đã tồn tại.");
    private ObjectResult ConcurrentChange() => Problem(statusCode: 409, title: "Sản phẩm đã được thay đổi hoặc xóa. Vui lòng tải lại và thử lại.");
    private static SanPhamResponse ToResponse(SanPham item) => new(item.MaSanPham, item.MaDanhMuc, item.SKU,
        item.TenSanPham, item.DonViTinh, item.GiaBan, item.TrangThai, item.NgayTao);
}
