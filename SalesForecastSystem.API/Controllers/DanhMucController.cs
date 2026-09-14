using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.Authorization;
using SalesForecastSystem.Core.DTOs.DanhMuc;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.API.Controllers;

[ApiController]
[Route("api/danh-muc")]
[Authorize(Roles = AppRoles.All)]
public class DanhMucController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<DanhMucResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await context.DanhMucs.AsNoTracking().OrderBy(x => x.MaDanhMuc)
            .Select(x => new DanhMucResponse(x.MaDanhMuc, x.TenDanhMuc, x.MoTa, x.TrangThai))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DanhMucResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var item = await context.DanhMucs.AsNoTracking().SingleOrDefaultAsync(x => x.MaDanhMuc == id, cancellationToken);
        return item is null ? Missing() : Ok(ToResponse(item));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(DanhMucResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(DanhMucRequest request, CancellationToken cancellationToken)
    {
        var item = new DanhMuc
        {
            TenDanhMuc = request.TenDanhMuc.Trim(), MoTa = request.MoTa?.Trim(), TrangThai = request.TrangThai
        };
        context.DanhMucs.Add(item);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) when (IsDuplicate(ex)) { return Duplicate(); }
        return CreatedAtAction(nameof(Detail), new { id = item.MaDanhMuc }, ToResponse(item));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(DanhMucResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, DanhMucRequest request, CancellationToken cancellationToken)
    {
        var item = await context.DanhMucs.SingleOrDefaultAsync(x => x.MaDanhMuc == id, cancellationToken);
        if (item is null) return Missing();
        item.TenDanhMuc = request.TenDanhMuc.Trim();
        item.MoTa = request.MoTa?.Trim();
        item.TrangThai = request.TrangThai;
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Missing(); }
        catch (DbUpdateException ex) when (IsDuplicate(ex)) { return Duplicate(); }
        return Ok(ToResponse(item));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var item = await context.DanhMucs.SingleOrDefaultAsync(x => x.MaDanhMuc == id, cancellationToken);
        if (item is null) return Missing();
        context.DanhMucs.Remove(item);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Missing(); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            return Problem(statusCode: 409, title: "Danh mục đang có sản phẩm, không thể xóa.");
        }
        return Ok(new { message = "Xóa danh mục thành công." });
    }

    private ObjectResult Missing() => Problem(statusCode: 404, title: "Không tìm thấy danh mục.");
    private ObjectResult Duplicate() => Problem(statusCode: 409, title: "Tên danh mục đã tồn tại.");
    private static bool IsDuplicate(DbUpdateException ex) => ex.InnerException is SqlException { Number: 2601 or 2627 };
    private static DanhMucResponse ToResponse(DanhMuc item) => new(item.MaDanhMuc, item.TenDanhMuc, item.MoTa, item.TrangThai);
}
