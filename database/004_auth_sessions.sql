USE SalesForecastingDB;
GO
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersion WHERE Version = 2)
BEGIN
    CREATE TABLE dbo.PhienDangNhap (
        MaPhien uniqueidentifier NOT NULL CONSTRAINT PK_PhienDangNhap PRIMARY KEY,
        MaNgDung int NOT NULL REFERENCES dbo.NguoiDung(MaNgDung),
        NgayTao datetime2 NOT NULL,
        HetHanLuc datetime2 NOT NULL,
        ThuHoiLuc datetime2 NULL,
        CONSTRAINT CK_PhienDangNhap_HetHan CHECK (HetHanLuc > NgayTao)
    );
    CREATE INDEX IX_PhienDangNhap_MaNgDung_HetHanLuc ON dbo.PhienDangNhap(MaNgDung, HetHanLuc);
    INSERT dbo.SchemaVersion(Version) VALUES (2);
END;
GRANT SELECT, INSERT, UPDATE ON dbo.PhienDangNhap TO SalesForecastApp;
COMMIT;
GO
