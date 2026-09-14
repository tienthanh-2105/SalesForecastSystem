-- Run with sqlcmd -E -C -b -f 65001 -S .\SQLEXPRESS -i database/001_schema.sql
USE master;
GO
IF DB_ID(N'SalesForecastingDB') IS NULL
    EXEC(N'CREATE DATABASE SalesForecastingDB');
GO
USE SalesForecastingDB;
GO
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.SchemaVersion', N'U') IS NULL
    CREATE TABLE dbo.SchemaVersion (Version int NOT NULL PRIMARY KEY, AppliedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersion WHERE Version = 1)
BEGIN
    -- Existing authentication entities: names and types match AppDbContext.
    CREATE TABLE dbo.VaiTro (
        MaVaiTro int IDENTITY PRIMARY KEY,
        TenVaiTro nvarchar(50) NOT NULL,
        MoTa nvarchar(255) NULL,
        TrangThai bit NOT NULL CONSTRAINT DF_VaiTro_TrangThai DEFAULT 1,
        NgayTao datetime NOT NULL CONSTRAINT DF_VaiTro_NgayTao DEFAULT GETUTCDATE()
    );
    CREATE UNIQUE INDEX IX_VaiTro_TenVaiTro ON dbo.VaiTro(TenVaiTro);
    CREATE TABLE dbo.NguoiDung (
        MaNgDung int IDENTITY PRIMARY KEY,
        MaVaiTro int NOT NULL REFERENCES dbo.VaiTro(MaVaiTro),
        HoTen nvarchar(100) NOT NULL,
        Email varchar(100) NOT NULL,
        MatKhau varchar(255) NOT NULL,
        SoDienThoai varchar(15) NULL,
        TrangThai nvarchar(20) NOT NULL CONSTRAINT DF_NguoiDung_TrangThai DEFAULT N'Hoạt động',
        NgayTao datetime NOT NULL CONSTRAINT DF_NguoiDung_NgayTao DEFAULT GETUTCDATE(),
        NgayCapNhat datetime NULL,
        CONSTRAINT CK_NguoiDung_TrangThai CHECK (TrangThai IN (N'Hoạt động', N'Bị khóa'))
    );
    CREATE UNIQUE INDEX IX_NguoiDung_Email ON dbo.NguoiDung(Email);
    CREATE INDEX IX_NguoiDung_MaVaiTro ON dbo.NguoiDung(MaVaiTro);
    CREATE INDEX IX_NguoiDung_TrangThai ON dbo.NguoiDung(TrangThai);

    CREATE TABLE dbo.DanhMuc (
        MaDanhMuc int IDENTITY PRIMARY KEY, TenDanhMuc nvarchar(100) NOT NULL UNIQUE,
        MoTa nvarchar(500) NULL, TrangThai bit NOT NULL DEFAULT 1
    );
    CREATE TABLE dbo.SanPham (
        MaSanPham int IDENTITY PRIMARY KEY, MaDanhMuc int NOT NULL REFERENCES dbo.DanhMuc(MaDanhMuc),
        SKU varchar(50) NOT NULL UNIQUE, TenSanPham nvarchar(200) NOT NULL,
        DonViTinh nvarchar(30) NOT NULL, GiaBan decimal(18,2) NOT NULL CHECK (GiaBan >= 0),
        TrangThai bit NOT NULL DEFAULT 1, NgayTao datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
        PhienBan rowversion NOT NULL
    );
    CREATE INDEX IX_SanPham_MaDanhMuc ON dbo.SanPham(MaDanhMuc);
    CREATE TABLE dbo.Kho (
        MaKho int IDENTITY PRIMARY KEY, TenKho nvarchar(100) NOT NULL UNIQUE,
        DiaChi nvarchar(255) NULL, TrangThai bit NOT NULL DEFAULT 1
    );
    CREATE TABLE dbo.KhachHang (
        MaKhachHang int IDENTITY PRIMARY KEY, HoTen nvarchar(100) NOT NULL,
        Email varchar(100) NULL, SoDienThoai varchar(15) NULL, DiaChi nvarchar(255) NULL,
        NgayTao datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_KhachHang_SoDienThoai ON dbo.KhachHang(SoDienThoai);
    CREATE TABLE dbo.NhaCungCap (
        MaNhaCungCap int IDENTITY PRIMARY KEY, TenNhaCungCap nvarchar(200) NOT NULL,
        MaSoThue varchar(20) NULL, Email varchar(100) NULL, SoDienThoai varchar(15) NULL,
        DiaChi nvarchar(255) NULL, TrangThai bit NOT NULL DEFAULT 1
    );
    CREATE UNIQUE INDEX IX_NhaCungCap_MaSoThue ON dbo.NhaCungCap(MaSoThue) WHERE MaSoThue IS NOT NULL;
    CREATE TABLE dbo.PhieuNhap (
        MaPhieuNhap bigint IDENTITY PRIMARY KEY, SoPhieu varchar(50) NOT NULL UNIQUE,
        MaKho int NOT NULL REFERENCES dbo.Kho(MaKho),
        MaNhaCungCap int NOT NULL REFERENCES dbo.NhaCungCap(MaNhaCungCap),
        MaNgDung int NOT NULL REFERENCES dbo.NguoiDung(MaNgDung),
        NgayNhap date NOT NULL, TrangThai varchar(20) NOT NULL DEFAULT 'Nhap',
        GhiChu nvarchar(500) NULL, NgayTao datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
        NgayXacNhan datetime2 NULL, PhienBan rowversion NOT NULL,
        CONSTRAINT CK_PhieuNhap_TrangThai CHECK (TrangThai IN ('Nhap','DaNhap','Huy')),
        CONSTRAINT CK_PhieuNhap_XacNhan CHECK ((TrangThai = 'DaNhap' AND NgayXacNhan IS NOT NULL) OR (TrangThai <> 'DaNhap' AND NgayXacNhan IS NULL))
    );
    CREATE INDEX IX_PhieuNhap_Kho_Ngay ON dbo.PhieuNhap(MaKho, NgayNhap);
    CREATE INDEX IX_PhieuNhap_NCC ON dbo.PhieuNhap(MaNhaCungCap);
    CREATE INDEX IX_PhieuNhap_NguoiDung ON dbo.PhieuNhap(MaNgDung);
    CREATE TABLE dbo.ChiTietPhieuNhap (
        MaChiTietPhieuNhap bigint IDENTITY PRIMARY KEY,
        MaPhieuNhap bigint NOT NULL REFERENCES dbo.PhieuNhap(MaPhieuNhap),
        MaSanPham int NOT NULL REFERENCES dbo.SanPham(MaSanPham),
        SoLuong int NOT NULL CHECK (SoLuong > 0), DonGia decimal(18,2) NOT NULL CHECK (DonGia >= 0),
        ThanhTien AS (CONVERT(decimal(28,2), SoLuong * DonGia)) PERSISTED,
        CONSTRAINT UQ_ChiTietPhieuNhap UNIQUE (MaPhieuNhap, MaSanPham)
    );
    CREATE INDEX IX_ChiTietPhieuNhap_SanPham ON dbo.ChiTietPhieuNhap(MaSanPham);
    CREATE TABLE dbo.DonHang (
        MaDonHang bigint IDENTITY PRIMARY KEY, SoDonHang varchar(50) NOT NULL UNIQUE,
        MaKho int NOT NULL REFERENCES dbo.Kho(MaKho),
        MaKhachHang int NULL REFERENCES dbo.KhachHang(MaKhachHang),
        MaNgDung int NOT NULL REFERENCES dbo.NguoiDung(MaNgDung),
        NgayBan date NOT NULL, TrangThai varchar(20) NOT NULL DEFAULT 'Nhap',
        GhiChu nvarchar(500) NULL, NgayTao datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
        NgayXacNhan datetime2 NULL, PhienBan rowversion NOT NULL,
        CONSTRAINT CK_DonHang_TrangThai CHECK (TrangThai IN ('Nhap','HoanTat','Huy')),
        CONSTRAINT CK_DonHang_XacNhan CHECK ((TrangThai = 'HoanTat' AND NgayXacNhan IS NOT NULL) OR (TrangThai <> 'HoanTat' AND NgayXacNhan IS NULL))
    );
    CREATE INDEX IX_DonHang_Kho_Ngay ON dbo.DonHang(MaKho, NgayBan) INCLUDE (TrangThai);
    CREATE INDEX IX_DonHang_KhachHang ON dbo.DonHang(MaKhachHang);
    CREATE INDEX IX_DonHang_NguoiDung ON dbo.DonHang(MaNgDung);
    CREATE TABLE dbo.ChiTietDonHang (
        MaChiTietDonHang bigint IDENTITY PRIMARY KEY,
        MaDonHang bigint NOT NULL REFERENCES dbo.DonHang(MaDonHang),
        MaSanPham int NOT NULL REFERENCES dbo.SanPham(MaSanPham),
        SoLuong int NOT NULL CHECK (SoLuong > 0), DonGia decimal(18,2) NOT NULL CHECK (DonGia >= 0),
        GiamGia decimal(18,2) NOT NULL DEFAULT 0,
        ThanhTien AS (CONVERT(decimal(28,2), SoLuong * DonGia - GiamGia)) PERSISTED,
        CONSTRAINT CK_ChiTietDonHang_GiamGia CHECK (GiamGia >= 0 AND GiamGia <= SoLuong * DonGia),
        CONSTRAINT UQ_ChiTietDonHang UNIQUE (MaDonHang, MaSanPham)
    );
    CREATE INDEX IX_ChiTietDonHang_SanPham ON dbo.ChiTietDonHang(MaSanPham);
    -- Stock is derived from a ledger; no duplicated mutable stock balance.
    CREATE TABLE dbo.GiaoDichKho (
        MaGiaoDich bigint IDENTITY PRIMARY KEY,
        MaKho int NOT NULL REFERENCES dbo.Kho(MaKho),
        MaSanPham int NOT NULL REFERENCES dbo.SanPham(MaSanPham),
        MaChiTietPhieuNhap bigint NULL REFERENCES dbo.ChiTietPhieuNhap(MaChiTietPhieuNhap),
        MaChiTietDonHang bigint NULL REFERENCES dbo.ChiTietDonHang(MaChiTietDonHang),
        SoLuong int NOT NULL, NgayGiaoDich date NOT NULL,
        NgayTao datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_GiaoDichKho_Nguon CHECK (
            (MaChiTietPhieuNhap IS NOT NULL AND MaChiTietDonHang IS NULL AND SoLuong > 0) OR
            (MaChiTietPhieuNhap IS NULL AND MaChiTietDonHang IS NOT NULL AND SoLuong < 0))
    );
    CREATE UNIQUE INDEX IX_GiaoDichKho_PhieuNhap ON dbo.GiaoDichKho(MaChiTietPhieuNhap) WHERE MaChiTietPhieuNhap IS NOT NULL;
    CREATE UNIQUE INDEX IX_GiaoDichKho_DonHang ON dbo.GiaoDichKho(MaChiTietDonHang) WHERE MaChiTietDonHang IS NOT NULL;
    CREATE INDEX IX_GiaoDichKho_Kho_SanPham_Ngay ON dbo.GiaoDichKho(MaKho, MaSanPham, NgayGiaoDich) INCLUDE (SoLuong);
    CREATE TABLE dbo.MoHinhDuBao (
        MaMoHinh int IDENTITY PRIMARY KEY, TenMoHinh nvarchar(100) NOT NULL,
        PhienBan varchar(50) NOT NULL, ThamSo nvarchar(max) NULL CHECK (ThamSo IS NULL OR ISJSON(ThamSo) = 1),
        MoTa nvarchar(500) NULL, CONSTRAINT UQ_MoHinh_PhienBan UNIQUE (TenMoHinh, PhienBan)
    );
    CREATE TABLE dbo.LanChayDuBao (
        MaLanChay bigint IDENTITY PRIMARY KEY,
        MaMoHinh int NOT NULL REFERENCES dbo.MoHinhDuBao(MaMoHinh),
        MaKho int NOT NULL REFERENCES dbo.Kho(MaKho),
        MaNgDung int NULL REFERENCES dbo.NguoiDung(MaNgDung),
        TuNgayDuLieu date NOT NULL, DenNgayDuLieu date NOT NULL,
        TuNgayDuBao date NOT NULL, DenNgayDuBao date NOT NULL,
        TrangThai varchar(20) NOT NULL DEFAULT 'ChoChay' CHECK (TrangThai IN ('ChoChay','DangChay','HoanTat','Loi')),
        MAE decimal(18,4) NULL CHECK (MAE >= 0), RMSE decimal(18,4) NULL CHECK (RMSE >= 0),
        ThongBaoLoi nvarchar(2000) NULL, NgayTao datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
        NgayHoanTat datetime2 NULL,
        CONSTRAINT CK_LanChay_Ngay CHECK (TuNgayDuLieu <= DenNgayDuLieu AND DenNgayDuLieu < TuNgayDuBao AND TuNgayDuBao <= DenNgayDuBao)
    );
    CREATE INDEX IX_LanChay_Kho_Ngay ON dbo.LanChayDuBao(MaKho, NgayTao);
    CREATE INDEX IX_LanChay_MoHinh ON dbo.LanChayDuBao(MaMoHinh);
    CREATE INDEX IX_LanChay_NguoiDung ON dbo.LanChayDuBao(MaNgDung);
    CREATE TABLE dbo.KetQuaDuBao (
        MaLanChay bigint NOT NULL REFERENCES dbo.LanChayDuBao(MaLanChay),
        MaSanPham int NOT NULL REFERENCES dbo.SanPham(MaSanPham), NgayDuBao date NOT NULL,
        SoLuongDuBao decimal(18,4) NOT NULL CHECK (SoLuongDuBao >= 0),
        DoanhThuDuBao decimal(18,2) NOT NULL CHECK (DoanhThuDuBao >= 0),
        CanDuoi decimal(18,4) NULL, CanTren decimal(18,4) NULL,
        CONSTRAINT PK_KetQuaDuBao PRIMARY KEY (MaLanChay, MaSanPham, NgayDuBao),
        CONSTRAINT CK_KetQua_Khoang CHECK (
            (CanDuoi IS NULL AND CanTren IS NULL) OR
            (CanDuoi IS NOT NULL AND CanTren IS NOT NULL AND CanDuoi >= 0 AND CanDuoi <= SoLuongDuBao AND SoLuongDuBao <= CanTren))
    );
    CREATE INDEX IX_KetQua_SanPham_Ngay ON dbo.KetQuaDuBao(MaSanPham, NgayDuBao);
    INSERT dbo.VaiTro(TenVaiTro, MoTa) VALUES
        (N'Admin', N'Quản trị hệ thống'),
        (N'Quản lý kho', N'Quản lý nhập hàng và tồn kho'),
        (N'Nhân viên bán hàng', N'Quản lý khách hàng và đơn bán');
    INSERT dbo.SchemaVersion(Version) VALUES (1);
END;
COMMIT;
GO
