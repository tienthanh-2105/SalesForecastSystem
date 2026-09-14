namespace SalesForecastSystem.Core.DTOs.DanhMuc;

public record DanhMucResponse(int MaDanhMuc, string TenDanhMuc, string? MoTa, bool TrangThai);
