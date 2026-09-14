USE SalesForecastingDB;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER VIEW dbo.vw_TonKho AS
SELECT MaKho, MaSanPham, SUM(CONVERT(bigint, SoLuong)) AS SoLuongTon
FROM dbo.GiaoDichKho GROUP BY MaKho, MaSanPham;
GO
CREATE OR ALTER VIEW dbo.vw_DoanhSoTheoNgay AS
SELECT d.MaKho, c.MaSanPham, d.NgayBan,
       SUM(CONVERT(bigint, c.SoLuong)) AS SoLuongBan,
       SUM(c.ThanhTien) AS DoanhThu,
       COUNT_BIG(DISTINCT d.MaDonHang) AS SoDonHang
FROM dbo.DonHang d JOIN dbo.ChiTietDonHang c ON c.MaDonHang = d.MaDonHang
WHERE d.TrangThai = 'HoanTat'
GROUP BY d.MaKho, c.MaSanPham, d.NgayBan;
GO
CREATE OR ALTER VIEW dbo.vw_TongTienDonHang AS
SELECT d.MaDonHang, d.SoDonHang, d.TrangThai,
       COALESCE(SUM(c.ThanhTien), 0) AS TongTien
FROM dbo.DonHang d LEFT JOIN dbo.ChiTietDonHang c ON c.MaDonHang = d.MaDonHang
GROUP BY d.MaDonHang, d.SoDonHang, d.TrangThai;
GO
CREATE OR ALTER PROCEDURE dbo.usp_XacNhanPhieuNhap @MaPhieuNhap bigint
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @MaKho int, @Ngay date, @TrangThai varchar(20), @LockResult int, @Resource nvarchar(255);
        SELECT @MaKho = MaKho, @Ngay = NgayNhap, @TrangThai = TrangThai
        FROM dbo.PhieuNhap WITH (UPDLOCK, HOLDLOCK) WHERE MaPhieuNhap = @MaPhieuNhap;
        IF @MaKho IS NULL THROW 51001, N'Không tìm thấy phiếu nhập.', 1;
        IF @TrangThai = 'DaNhap' BEGIN COMMIT; RETURN; END;
        IF @TrangThai <> 'Nhap' THROW 51002, N'Chỉ xác nhận phiếu ở trạng thái Nhap.', 1;
        SET @Resource = CONCAT(N'TonKho:', @MaKho);
        EXEC @LockResult = sys.sp_getapplock @Resource = @Resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
        IF @LockResult < 0 THROW 51003, N'Kho đang được cập nhật, vui lòng thử lại.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.Kho WHERE MaKho = @MaKho AND TrangThai = 1)
            THROW 51004, N'Kho đã ngừng hoạt động.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.ChiTietPhieuNhap WITH (UPDLOCK, HOLDLOCK) WHERE MaPhieuNhap = @MaPhieuNhap)
            THROW 51005, N'Phiếu nhập chưa có sản phẩm.', 1;
        IF EXISTS (SELECT 1 FROM dbo.ChiTietPhieuNhap c JOIN dbo.SanPham s ON s.MaSanPham = c.MaSanPham WHERE c.MaPhieuNhap = @MaPhieuNhap AND s.TrangThai = 0)
            THROW 51006, N'Sản phẩm đã ngừng hoạt động.', 1;
        UPDATE dbo.PhieuNhap SET TrangThai = 'DaNhap', NgayXacNhan = SYSUTCDATETIME() WHERE MaPhieuNhap = @MaPhieuNhap;
        INSERT dbo.GiaoDichKho(MaKho, MaSanPham, MaChiTietPhieuNhap, SoLuong, NgayGiaoDich)
        SELECT @MaKho, MaSanPham, MaChiTietPhieuNhap, SoLuong, @Ngay
        FROM dbo.ChiTietPhieuNhap WHERE MaPhieuNhap = @MaPhieuNhap;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_HoanTatDonHang @MaDonHang bigint
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @MaKho int, @Ngay date, @TrangThai varchar(20), @LockResult int, @Resource nvarchar(255);
        SELECT @MaKho = MaKho, @Ngay = NgayBan, @TrangThai = TrangThai
        FROM dbo.DonHang WITH (UPDLOCK, HOLDLOCK) WHERE MaDonHang = @MaDonHang;
        IF @MaKho IS NULL THROW 51101, N'Không tìm thấy đơn hàng.', 1;
        IF @TrangThai = 'HoanTat' BEGIN COMMIT; RETURN; END;
        IF @TrangThai <> 'Nhap' THROW 51102, N'Chỉ hoàn tất đơn ở trạng thái Nhap.', 1;
        SET @Resource = CONCAT(N'TonKho:', @MaKho);
        EXEC @LockResult = sys.sp_getapplock @Resource = @Resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
        IF @LockResult < 0 THROW 51103, N'Kho đang được cập nhật, vui lòng thử lại.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.Kho WHERE MaKho = @MaKho AND TrangThai = 1)
            THROW 51104, N'Kho đã ngừng hoạt động.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.ChiTietDonHang WITH (UPDLOCK, HOLDLOCK) WHERE MaDonHang = @MaDonHang)
            THROW 51105, N'Đơn hàng chưa có sản phẩm.', 1;
        IF EXISTS (SELECT 1 FROM dbo.ChiTietDonHang c JOIN dbo.SanPham s ON s.MaSanPham = c.MaSanPham WHERE c.MaDonHang = @MaDonHang AND s.TrangThai = 0)
            THROW 51106, N'Sản phẩm đã ngừng hoạt động.', 1;
        IF EXISTS (
            SELECT 1 FROM dbo.ChiTietDonHang c
            LEFT JOIN dbo.vw_TonKho t ON t.MaKho = @MaKho AND t.MaSanPham = c.MaSanPham
            WHERE c.MaDonHang = @MaDonHang AND c.SoLuong > COALESCE(t.SoLuongTon, 0)
        ) THROW 51107, N'Không đủ tồn kho để hoàn tất đơn hàng.', 1;
        UPDATE dbo.DonHang SET TrangThai = 'HoanTat', NgayXacNhan = SYSUTCDATETIME() WHERE MaDonHang = @MaDonHang;
        INSERT dbo.GiaoDichKho(MaKho, MaSanPham, MaChiTietDonHang, SoLuong, NgayGiaoDich)
        SELECT @MaKho, MaSanPham, MaChiTietDonHang, -SoLuong, @Ngay
        FROM dbo.ChiTietDonHang WHERE MaDonHang = @MaDonHang;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH;
END;
GO
-- Freeze posted/cancelled documents to keep history and stock consistent.
CREATE OR ALTER TRIGGER dbo.TR_PhieuNhap_BaoVe ON dbo.PhieuNhap AFTER UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted WHERE TrangThai <> 'Nhap')
        THROW 51201, N'Không được sửa hoặc xóa phiếu nhập đã xác nhận/hủy.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_DonHang_BaoVe ON dbo.DonHang AFTER UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted WHERE TrangThai <> 'Nhap')
        THROW 51202, N'Không được sửa hoặc xóa đơn hàng đã hoàn tất/hủy.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_ChiTietPhieuNhap_BaoVe ON dbo.ChiTietPhieuNhap AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.PhieuNhap p WITH (UPDLOCK, HOLDLOCK)
        JOIN (SELECT MaPhieuNhap FROM inserted UNION SELECT MaPhieuNhap FROM deleted) c ON c.MaPhieuNhap = p.MaPhieuNhap
        WHERE p.TrangThai <> 'Nhap')
        THROW 51203, N'Chỉ được sửa chi tiết phiếu nhập nháp.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_ChiTietDonHang_BaoVe ON dbo.ChiTietDonHang AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.DonHang d WITH (UPDLOCK, HOLDLOCK)
        JOIN (SELECT MaDonHang FROM inserted UNION SELECT MaDonHang FROM deleted) c ON c.MaDonHang = d.MaDonHang
        WHERE d.TrangThai <> 'Nhap')
        THROW 51204, N'Chỉ được sửa chi tiết đơn hàng nháp.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_GiaoDichKho_BaoVe ON dbo.GiaoDichKho AFTER UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted)
        THROW 51205, N'Không được sửa hoặc xóa lịch sử giao dịch kho.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_KetQuaDuBao_Ngay ON dbo.KetQuaDuBao AFTER INSERT, UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.LanChayDuBao l ON l.MaLanChay = i.MaLanChay
        WHERE i.NgayDuBao < l.TuNgayDuBao OR i.NgayDuBao > l.DenNgayDuBao)
        THROW 51206, N'Ngày kết quả phải nằm trong khoảng dự báo của lần chạy.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_LanChayDuBao_Ngay ON dbo.LanChayDuBao AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.KetQuaDuBao k ON k.MaLanChay = i.MaLanChay
        WHERE k.NgayDuBao < i.TuNgayDuBao OR k.NgayDuBao > i.DenNgayDuBao)
        THROW 51209, N'Khoảng dự báo mới không được loại trừ kết quả đã lưu.', 1;
END;
GO
-- Application role: posting must go through the procedures (ownership chaining).
IF DATABASE_PRINCIPAL_ID(N'SalesForecastApp') IS NULL CREATE ROLE SalesForecastApp;
GRANT SELECT ON SCHEMA::dbo TO SalesForecastApp;
GRANT INSERT, UPDATE, DELETE ON dbo.DanhMuc TO SalesForecastApp;
GRANT INSERT, UPDATE, DELETE ON dbo.SanPham TO SalesForecastApp;
GRANT INSERT, UPDATE, DELETE ON dbo.Kho TO SalesForecastApp;
GRANT INSERT, UPDATE, DELETE ON dbo.KhachHang TO SalesForecastApp;
GRANT INSERT, UPDATE, DELETE ON dbo.NhaCungCap TO SalesForecastApp;
GRANT INSERT, DELETE ON dbo.PhieuNhap TO SalesForecastApp;
GRANT UPDATE ON dbo.PhieuNhap (SoPhieu, MaKho, MaNhaCungCap, MaNgDung, NgayNhap, GhiChu) TO SalesForecastApp;
GRANT INSERT, DELETE ON dbo.DonHang TO SalesForecastApp;
GRANT UPDATE ON dbo.DonHang (SoDonHang, MaKho, MaKhachHang, MaNgDung, NgayBan, GhiChu) TO SalesForecastApp;
GRANT INSERT, UPDATE, DELETE ON dbo.ChiTietPhieuNhap TO SalesForecastApp;
GRANT INSERT, UPDATE, DELETE ON dbo.ChiTietDonHang TO SalesForecastApp;
GRANT INSERT, UPDATE ON dbo.MoHinhDuBao TO SalesForecastApp;
GRANT INSERT, UPDATE ON dbo.LanChayDuBao TO SalesForecastApp;
GRANT INSERT, UPDATE, DELETE ON dbo.KetQuaDuBao TO SalesForecastApp;
GRANT EXECUTE ON dbo.usp_XacNhanPhieuNhap TO SalesForecastApp;
GRANT EXECUTE ON dbo.usp_HoanTatDonHang TO SalesForecastApp;
GO
CREATE OR ALTER TRIGGER dbo.TR_PhieuNhap_NhapMoi ON dbo.PhieuNhap AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted WHERE TrangThai <> 'Nhap')
        THROW 51207, N'Phiếu nhập mới phải ở trạng thái Nhap.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_DonHang_NhapMoi ON dbo.DonHang AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted WHERE TrangThai <> 'Nhap')
        THROW 51208, N'Đơn hàng mới phải ở trạng thái Nhap.', 1;
END;
GO
