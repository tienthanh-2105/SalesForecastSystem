-- All business test records are rolled back. Identity values may have gaps.
USE SalesForecastingDB;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_WARNINGS ON;
SET ANSI_PADDING ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO
CREATE PROCEDURE #Fixture @SaleQty int, @OrderId bigint OUTPUT, @ReceiptId bigint OUTPUT, @WarehouseId int OUTPUT, @ProductId int OUTPUT
AS
BEGIN
    DECLARE @UserId int, @CategoryId int, @SupplierId int;
    DECLARE @Tag varchar(36) = CONVERT(varchar(36), NEWID());
    INSERT dbo.NguoiDung(MaVaiTro, HoTen, Email, MatKhau, TrangThai)
    SELECT MaVaiTro, N'Kiểm thử rollback', CONCAT(@Tag, '@example.invalid'), 'test-only-not-a-login', N'Bị khóa'
    FROM dbo.VaiTro WHERE TenVaiTro = N'Admin';
    SET @UserId = SCOPE_IDENTITY();
    INSERT dbo.DanhMuc(TenDanhMuc) VALUES (@Tag);
    SET @CategoryId = SCOPE_IDENTITY();
    INSERT dbo.SanPham(MaDanhMuc, SKU, TenSanPham, DonViTinh, GiaBan) VALUES (@CategoryId, @Tag, N'Sản phẩm kiểm thử', N'Cái', 10000);
    SET @ProductId = SCOPE_IDENTITY();
    INSERT dbo.Kho(TenKho) VALUES (@Tag);
    SET @WarehouseId = SCOPE_IDENTITY();
    INSERT dbo.NhaCungCap(TenNhaCungCap) VALUES (@Tag);
    SET @SupplierId = SCOPE_IDENTITY();
    INSERT dbo.PhieuNhap(SoPhieu, MaKho, MaNhaCungCap, MaNgDung, NgayNhap) VALUES (@Tag, @WarehouseId, @SupplierId, @UserId, '20260901');
    SET @ReceiptId = SCOPE_IDENTITY();
    INSERT dbo.ChiTietPhieuNhap(MaPhieuNhap, MaSanPham, SoLuong, DonGia) VALUES (@ReceiptId, @ProductId, 10, 6000);
    INSERT dbo.DonHang(SoDonHang, MaKho, MaNgDung, NgayBan) VALUES (@Tag, @WarehouseId, @UserId, '20260902');
    SET @OrderId = SCOPE_IDENTITY();
    INSERT dbo.ChiTietDonHang(MaDonHang, MaSanPham, SoLuong, DonGia, GiamGia) VALUES (@OrderId, @ProductId, @SaleQty, 10000, 2000);
END;
GO
DECLARE @Order bigint, @Receipt bigint, @Warehouse int, @Product int;
BEGIN TRY
    BEGIN TRANSACTION;
    EXEC #Fixture 3, @Order OUTPUT, @Receipt OUTPUT, @Warehouse OUTPUT, @Product OUTPUT;
    EXEC dbo.usp_XacNhanPhieuNhap @Receipt;
    EXEC dbo.usp_XacNhanPhieuNhap @Receipt;
    EXEC dbo.usp_HoanTatDonHang @Order;
    EXEC dbo.usp_HoanTatDonHang @Order;
    IF (SELECT SoLuongTon FROM dbo.vw_TonKho WHERE MaKho = @Warehouse AND MaSanPham = @Product) <> 7
        THROW 51901, 'Incorrect stock balance.', 1;
    IF (SELECT COUNT(*) FROM dbo.GiaoDichKho WHERE MaKho = @Warehouse) <> 2
        THROW 51902, 'Duplicate ledger posting.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.vw_DoanhSoTheoNgay WHERE MaKho = @Warehouse AND SoLuongBan = 3 AND DoanhThu = 28000)
        THROW 51903, 'Incorrect daily revenue.', 1;
    ROLLBACK;
    PRINT 'PASS: receipt, sale, stock = 7, revenue = 28000, retry is idempotent.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    THROW;
END CATCH;

BEGIN TRY
    BEGIN TRANSACTION;
    EXEC #Fixture 11, @Order OUTPUT, @Receipt OUTPUT, @Warehouse OUTPUT, @Product OUTPUT;
    EXEC dbo.usp_XacNhanPhieuNhap @Receipt;
    EXEC dbo.usp_HoanTatDonHang @Order;
    THROW 51904, 'Overselling was not rejected.', 1;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() <> 51107 THROW;
    PRINT 'PASS: overselling rejected and transaction rolled back.';
END CATCH;

BEGIN TRY
    BEGIN TRANSACTION;
    EXEC #Fixture 3, @Order OUTPUT, @Receipt OUTPUT, @Warehouse OUTPUT, @Product OUTPUT;
    EXEC dbo.usp_XacNhanPhieuNhap @Receipt;
    EXEC dbo.usp_HoanTatDonHang @Order;
    UPDATE dbo.ChiTietDonHang SET SoLuong = 2 WHERE MaDonHang = @Order;
    THROW 51905, 'Posted detail update was not rejected.', 1;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() <> 51204 THROW;
    PRINT 'PASS: posted sales details are immutable.';
END CATCH;

BEGIN TRY
    BEGIN TRANSACTION;
    EXEC #Fixture 3, @Order OUTPUT, @Receipt OUTPUT, @Warehouse OUTPUT, @Product OUTPUT;
    UPDATE dbo.ChiTietDonHang SET SoLuong = 0 WHERE MaDonHang = @Order;
    THROW 51906, 'Zero quantity was not rejected.', 1;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() <> 547 THROW;
    PRINT 'PASS: non-positive quantity rejected.';
END CATCH;

BEGIN TRY
    BEGIN TRANSACTION;
    EXEC #Fixture 3, @Order OUTPUT, @Receipt OUTPUT, @Warehouse OUTPUT, @Product OUTPUT;
    INSERT dbo.MoHinhDuBao(TenMoHinh, PhienBan) VALUES (CONVERT(nvarchar(36), NEWID()), 'test');
    DECLARE @Model int = SCOPE_IDENTITY(), @Run bigint;
    INSERT dbo.LanChayDuBao(MaMoHinh, MaKho, TuNgayDuLieu, DenNgayDuLieu, TuNgayDuBao, DenNgayDuBao)
    VALUES (@Model, @Warehouse, '20260801', '20260831', '20260901', '20260907');
    SET @Run = SCOPE_IDENTITY();
    INSERT dbo.KetQuaDuBao(MaLanChay, MaSanPham, NgayDuBao, SoLuongDuBao, DoanhThuDuBao)
    VALUES (@Run, @Product, '20260908', 5, 50000);
    THROW 51907, 'Forecast outside horizon was not rejected.', 1;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() <> 51206 THROW;
    PRINT 'PASS: forecast date outside run horizon rejected.';
END CATCH;

SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName;
SELECT COUNT(*) AS TableCount FROM sys.tables;
SELECT name AS TableName FROM sys.tables ORDER BY name;
SELECT TenVaiTro FROM dbo.VaiTro ORDER BY MaVaiTro;
GO
