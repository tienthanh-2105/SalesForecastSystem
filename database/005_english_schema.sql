USE SalesForecastingDB;
GO
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NOT NULL RETURN;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.SchemaVersion', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.SchemaVersion WHERE Version = 3)
BEGIN
    DROP VIEW IF EXISTS dbo.vw_TonKho;
    DROP VIEW IF EXISTS dbo.vw_DoanhSoTheoNgay;
    DROP VIEW IF EXISTS dbo.vw_TongTienDonHang;
    DROP PROCEDURE IF EXISTS dbo.usp_XacNhanPhieuNhap;
    DROP PROCEDURE IF EXISTS dbo.usp_HoanTatDonHang;

    DROP TRIGGER IF EXISTS dbo.TR_PhieuNhap_BaoVe;
    DROP TRIGGER IF EXISTS dbo.TR_DonHang_BaoVe;
    DROP TRIGGER IF EXISTS dbo.TR_ChiTietPhieuNhap_BaoVe;
    DROP TRIGGER IF EXISTS dbo.TR_ChiTietDonHang_BaoVe;
    DROP TRIGGER IF EXISTS dbo.TR_GiaoDichKho_BaoVe;
    DROP TRIGGER IF EXISTS dbo.TR_KetQuaDuBao_Ngay;
    DROP TRIGGER IF EXISTS dbo.TR_LanChayDuBao_Ngay;
    DROP TRIGGER IF EXISTS dbo.TR_PhieuNhap_NhapMoi;
    DROP TRIGGER IF EXISTS dbo.TR_DonHang_NhapMoi;

    EXEC sp_rename N'dbo.SchemaVersion', N'SchemaVersions';
    EXEC sp_rename N'dbo.VaiTro', N'Roles';
    EXEC sp_rename N'dbo.NguoiDung', N'Users';
    EXEC sp_rename N'dbo.PhienDangNhap', N'LoginSessions';
    EXEC sp_rename N'dbo.DanhMuc', N'Categories';
    EXEC sp_rename N'dbo.SanPham', N'Products';
    EXEC sp_rename N'dbo.Kho', N'Warehouses';
    EXEC sp_rename N'dbo.KhachHang', N'Customers';
    EXEC sp_rename N'dbo.NhaCungCap', N'Suppliers';
    EXEC sp_rename N'dbo.PhieuNhap', N'PurchaseOrders';
    EXEC sp_rename N'dbo.ChiTietPhieuNhap', N'PurchaseOrderItems';
    EXEC sp_rename N'dbo.DonHang', N'SalesOrders';
    EXEC sp_rename N'dbo.ChiTietDonHang', N'SalesOrderItems';
    EXEC sp_rename N'dbo.GiaoDichKho', N'InventoryTransactions';
    EXEC sp_rename N'dbo.MoHinhDuBao', N'ForecastModels';
    EXEC sp_rename N'dbo.LanChayDuBao', N'ForecastRuns';
    EXEC sp_rename N'dbo.KetQuaDuBao', N'ForecastResults';

    DECLARE @DropChecksBeforeRename nvarchar(max);
    SELECT @DropChecksBeforeRename = STRING_AGG(
        N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + N'.' +
        QUOTENAME(OBJECT_NAME(parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(name), N';')
    FROM sys.check_constraints
    WHERE OBJECT_NAME(parent_object_id) IN (
        N'Users', N'LoginSessions', N'Products', N'PurchaseOrders', N'PurchaseOrderItems', N'ForecastModels',
        N'SalesOrders', N'SalesOrderItems', N'InventoryTransactions', N'ForecastRuns', N'ForecastResults');
    IF @DropChecksBeforeRename IS NOT NULL EXEC sp_executesql @DropChecksBeforeRename;

    DECLARE @DropIndexesBeforeRename nvarchar(max);
    SELECT @DropIndexesBeforeRename = STRING_AGG(
        N'DROP INDEX ' + QUOTENAME(indexDefinition.name) + N' ON dbo.' + QUOTENAME(tableDefinition.name), N';')
    FROM sys.indexes AS indexDefinition
    JOIN sys.tables AS tableDefinition ON tableDefinition.object_id = indexDefinition.object_id
    WHERE SCHEMA_NAME(tableDefinition.schema_id) = N'dbo'
      AND tableDefinition.name IN (
        N'Roles', N'Users', N'LoginSessions', N'Products', N'PurchaseOrders', N'PurchaseOrderItems',
        N'SalesOrders', N'SalesOrderItems', N'InventoryTransactions', N'ForecastRuns', N'ForecastResults',
        N'Customers', N'Suppliers')
      AND indexDefinition.index_id > 0
      AND indexDefinition.is_primary_key = 0
      AND indexDefinition.is_unique_constraint = 0;
    IF @DropIndexesBeforeRename IS NOT NULL EXEC sp_executesql @DropIndexesBeforeRename;

    ALTER TABLE dbo.PurchaseOrderItems DROP COLUMN ThanhTien;
    ALTER TABLE dbo.SalesOrderItems DROP COLUMN ThanhTien;

    EXEC sp_rename N'dbo.Roles.MaVaiTro', N'RoleId', N'COLUMN';
    EXEC sp_rename N'dbo.Roles.TenVaiTro', N'Name', N'COLUMN';
    EXEC sp_rename N'dbo.Roles.MoTa', N'Description', N'COLUMN';
    EXEC sp_rename N'dbo.Roles.TrangThai', N'IsActive', N'COLUMN';
    EXEC sp_rename N'dbo.Roles.NgayTao', N'CreatedAt', N'COLUMN';

    EXEC sp_rename N'dbo.Users.MaNgDung', N'UserId', N'COLUMN';
    EXEC sp_rename N'dbo.Users.MaVaiTro', N'RoleId', N'COLUMN';
    EXEC sp_rename N'dbo.Users.HoTen', N'FullName', N'COLUMN';
    EXEC sp_rename N'dbo.Users.MatKhau', N'PasswordHash', N'COLUMN';
    EXEC sp_rename N'dbo.Users.SoDienThoai', N'PhoneNumber', N'COLUMN';
    EXEC sp_rename N'dbo.Users.TrangThai', N'Status', N'COLUMN';
    EXEC sp_rename N'dbo.Users.NgayTao', N'CreatedAt', N'COLUMN';
    EXEC sp_rename N'dbo.Users.NgayCapNhat', N'UpdatedAt', N'COLUMN';

    EXEC sp_rename N'dbo.LoginSessions.MaPhien', N'SessionId', N'COLUMN';
    EXEC sp_rename N'dbo.LoginSessions.MaNgDung', N'UserId', N'COLUMN';
    EXEC sp_rename N'dbo.LoginSessions.NgayTao', N'CreatedAt', N'COLUMN';
    EXEC sp_rename N'dbo.LoginSessions.HetHanLuc', N'ExpiresAt', N'COLUMN';
    EXEC sp_rename N'dbo.LoginSessions.ThuHoiLuc', N'RevokedAt', N'COLUMN';

    EXEC sp_rename N'dbo.Categories.MaDanhMuc', N'CategoryId', N'COLUMN';
    EXEC sp_rename N'dbo.Categories.TenDanhMuc', N'Name', N'COLUMN';
    EXEC sp_rename N'dbo.Categories.MoTa', N'Description', N'COLUMN';
    EXEC sp_rename N'dbo.Categories.TrangThai', N'IsActive', N'COLUMN';

    EXEC sp_rename N'dbo.Products.MaSanPham', N'ProductId', N'COLUMN';
    EXEC sp_rename N'dbo.Products.MaDanhMuc', N'CategoryId', N'COLUMN';
    EXEC sp_rename N'dbo.Products.TenSanPham', N'Name', N'COLUMN';
    EXEC sp_rename N'dbo.Products.DonViTinh', N'Unit', N'COLUMN';
    EXEC sp_rename N'dbo.Products.GiaBan', N'SalePrice', N'COLUMN';
    EXEC sp_rename N'dbo.Products.TrangThai', N'IsActive', N'COLUMN';
    EXEC sp_rename N'dbo.Products.NgayTao', N'CreatedAt', N'COLUMN';
    EXEC sp_rename N'dbo.Products.PhienBan', N'RowVersion', N'COLUMN';

    EXEC sp_rename N'dbo.Warehouses.MaKho', N'WarehouseId', N'COLUMN';
    EXEC sp_rename N'dbo.Warehouses.TenKho', N'Name', N'COLUMN';
    EXEC sp_rename N'dbo.Warehouses.DiaChi', N'Address', N'COLUMN';
    EXEC sp_rename N'dbo.Warehouses.TrangThai', N'IsActive', N'COLUMN';

    EXEC sp_rename N'dbo.Customers.MaKhachHang', N'CustomerId', N'COLUMN';
    EXEC sp_rename N'dbo.Customers.HoTen', N'FullName', N'COLUMN';
    EXEC sp_rename N'dbo.Customers.SoDienThoai', N'PhoneNumber', N'COLUMN';
    EXEC sp_rename N'dbo.Customers.DiaChi', N'Address', N'COLUMN';
    EXEC sp_rename N'dbo.Customers.NgayTao', N'CreatedAt', N'COLUMN';

    EXEC sp_rename N'dbo.Suppliers.MaNhaCungCap', N'SupplierId', N'COLUMN';
    EXEC sp_rename N'dbo.Suppliers.TenNhaCungCap', N'Name', N'COLUMN';
    EXEC sp_rename N'dbo.Suppliers.MaSoThue', N'TaxCode', N'COLUMN';
    EXEC sp_rename N'dbo.Suppliers.SoDienThoai', N'PhoneNumber', N'COLUMN';
    EXEC sp_rename N'dbo.Suppliers.DiaChi', N'Address', N'COLUMN';
    EXEC sp_rename N'dbo.Suppliers.TrangThai', N'IsActive', N'COLUMN';

    EXEC sp_rename N'dbo.PurchaseOrders.MaPhieuNhap', N'PurchaseOrderId', N'COLUMN';
    EXEC sp_rename N'dbo.PurchaseOrders.SoPhieu', N'OrderNumber', N'COLUMN';
    EXEC sp_rename N'dbo.PurchaseOrders.MaKho', N'WarehouseId', N'COLUMN';
    EXEC sp_rename N'dbo.PurchaseOrders.MaNhaCungCap', N'SupplierId', N'COLUMN';
    EXEC sp_rename N'dbo.PurchaseOrders.MaNgDung', N'CreatedByUserId', N'COLUMN';
    EXEC sp_rename N'dbo.PurchaseOrders.NgayNhap', N'OrderDate', N'COLUMN';
    EXEC sp_rename N'dbo.PurchaseOrders.TrangThai', N'Status', N'COLUMN';
    EXEC sp_rename N'dbo.PurchaseOrders.GhiChu', N'Notes', N'COLUMN';
    EXEC sp_rename N'dbo.PurchaseOrders.NgayTao', N'CreatedAt', N'COLUMN';
    EXEC sp_rename N'dbo.PurchaseOrders.NgayXacNhan', N'PostedAt', N'COLUMN';
    EXEC sp_rename N'dbo.PurchaseOrders.PhienBan', N'RowVersion', N'COLUMN';

    EXEC sp_rename N'dbo.PurchaseOrderItems.MaChiTietPhieuNhap', N'PurchaseOrderItemId', N'COLUMN';
    EXEC sp_rename N'dbo.PurchaseOrderItems.MaPhieuNhap', N'PurchaseOrderId', N'COLUMN';
    EXEC sp_rename N'dbo.PurchaseOrderItems.MaSanPham', N'ProductId', N'COLUMN';
    EXEC sp_rename N'dbo.PurchaseOrderItems.SoLuong', N'Quantity', N'COLUMN';
    EXEC sp_rename N'dbo.PurchaseOrderItems.DonGia', N'UnitPrice', N'COLUMN';

    EXEC sp_rename N'dbo.SalesOrders.MaDonHang', N'SalesOrderId', N'COLUMN';
    EXEC sp_rename N'dbo.SalesOrders.SoDonHang', N'OrderNumber', N'COLUMN';
    EXEC sp_rename N'dbo.SalesOrders.MaKho', N'WarehouseId', N'COLUMN';
    EXEC sp_rename N'dbo.SalesOrders.MaKhachHang', N'CustomerId', N'COLUMN';
    EXEC sp_rename N'dbo.SalesOrders.MaNgDung', N'CreatedByUserId', N'COLUMN';
    EXEC sp_rename N'dbo.SalesOrders.NgayBan', N'OrderDate', N'COLUMN';
    EXEC sp_rename N'dbo.SalesOrders.TrangThai', N'Status', N'COLUMN';
    EXEC sp_rename N'dbo.SalesOrders.GhiChu', N'Notes', N'COLUMN';
    EXEC sp_rename N'dbo.SalesOrders.NgayTao', N'CreatedAt', N'COLUMN';
    EXEC sp_rename N'dbo.SalesOrders.NgayXacNhan', N'CompletedAt', N'COLUMN';
    EXEC sp_rename N'dbo.SalesOrders.PhienBan', N'RowVersion', N'COLUMN';

    EXEC sp_rename N'dbo.SalesOrderItems.MaChiTietDonHang', N'SalesOrderItemId', N'COLUMN';
    EXEC sp_rename N'dbo.SalesOrderItems.MaDonHang', N'SalesOrderId', N'COLUMN';
    EXEC sp_rename N'dbo.SalesOrderItems.MaSanPham', N'ProductId', N'COLUMN';
    EXEC sp_rename N'dbo.SalesOrderItems.SoLuong', N'Quantity', N'COLUMN';
    EXEC sp_rename N'dbo.SalesOrderItems.DonGia', N'UnitPrice', N'COLUMN';
    EXEC sp_rename N'dbo.SalesOrderItems.GiamGia', N'Discount', N'COLUMN';

    EXEC sp_rename N'dbo.InventoryTransactions.MaGiaoDich', N'InventoryTransactionId', N'COLUMN';
    EXEC sp_rename N'dbo.InventoryTransactions.MaKho', N'WarehouseId', N'COLUMN';
    EXEC sp_rename N'dbo.InventoryTransactions.MaSanPham', N'ProductId', N'COLUMN';
    EXEC sp_rename N'dbo.InventoryTransactions.MaChiTietPhieuNhap', N'PurchaseOrderItemId', N'COLUMN';
    EXEC sp_rename N'dbo.InventoryTransactions.MaChiTietDonHang', N'SalesOrderItemId', N'COLUMN';
    EXEC sp_rename N'dbo.InventoryTransactions.SoLuong', N'Quantity', N'COLUMN';
    EXEC sp_rename N'dbo.InventoryTransactions.NgayGiaoDich', N'TransactionDate', N'COLUMN';
    EXEC sp_rename N'dbo.InventoryTransactions.NgayTao', N'CreatedAt', N'COLUMN';

    EXEC sp_rename N'dbo.ForecastModels.MaMoHinh', N'ForecastModelId', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastModels.TenMoHinh', N'Name', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastModels.PhienBan', N'Version', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastModels.ThamSo', N'Parameters', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastModels.MoTa', N'Description', N'COLUMN';

    EXEC sp_rename N'dbo.ForecastRuns.MaLanChay', N'ForecastRunId', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastRuns.MaMoHinh', N'ForecastModelId', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastRuns.MaKho', N'WarehouseId', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastRuns.MaNgDung', N'RequestedByUserId', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastRuns.TuNgayDuLieu', N'TrainingStartDate', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastRuns.DenNgayDuLieu', N'TrainingEndDate', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastRuns.TuNgayDuBao', N'ForecastStartDate', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastRuns.DenNgayDuBao', N'ForecastEndDate', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastRuns.TrangThai', N'Status', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastRuns.ThongBaoLoi', N'ErrorMessage', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastRuns.NgayTao', N'CreatedAt', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastRuns.NgayHoanTat', N'CompletedAt', N'COLUMN';

    EXEC sp_rename N'dbo.ForecastResults.MaLanChay', N'ForecastRunId', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastResults.MaSanPham', N'ProductId', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastResults.NgayDuBao', N'ForecastDate', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastResults.SoLuongDuBao', N'ForecastQuantity', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastResults.DoanhThuDuBao', N'ForecastRevenue', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastResults.CanDuoi', N'LowerBound', N'COLUMN';
    EXEC sp_rename N'dbo.ForecastResults.CanTren', N'UpperBound', N'COLUMN';

    ALTER TABLE dbo.PurchaseOrderItems ADD LineTotal AS (CONVERT(decimal(28,2), Quantity * UnitPrice)) PERSISTED;
    ALTER TABLE dbo.SalesOrderItems ADD LineTotal AS (CONVERT(decimal(28,2), Quantity * UnitPrice - Discount)) PERSISTED;

    UPDATE dbo.Roles SET Name = 'WarehouseManager' WHERE Name = N'Quản lý kho';
    UPDATE dbo.Roles SET Name = 'SalesStaff' WHERE Name = N'Nhân viên bán hàng';
    UPDATE dbo.Users SET Status = 'Active' WHERE Status = N'Hoạt động';
    UPDATE dbo.Users SET Status = 'Locked' WHERE Status = N'Bị khóa';
    UPDATE dbo.PurchaseOrders SET Status = CASE Status WHEN 'Nhap' THEN 'Draft' WHEN 'DaNhap' THEN 'Posted' WHEN 'Huy' THEN 'Cancelled' END;
    UPDATE dbo.SalesOrders SET Status = CASE Status WHEN 'Nhap' THEN 'Draft' WHEN 'HoanTat' THEN 'Completed' WHEN 'Huy' THEN 'Cancelled' END;
    UPDATE dbo.ForecastRuns SET Status = CASE Status WHEN 'ChoChay' THEN 'Pending' WHEN 'DangChay' THEN 'Running' WHEN 'HoanTat' THEN 'Completed' WHEN 'Loi' THEN 'Failed' END;

    ALTER TABLE dbo.Users ADD CONSTRAINT CK_Users_Status CHECK (Status IN ('Active', 'Locked'));
    ALTER TABLE dbo.LoginSessions ADD CONSTRAINT CK_LoginSessions_ExpiresAt CHECK (ExpiresAt > CreatedAt);
    ALTER TABLE dbo.Products ADD CONSTRAINT CK_Products_SalePrice CHECK (SalePrice >= 0);
    ALTER TABLE dbo.PurchaseOrders ADD CONSTRAINT CK_PurchaseOrders_Status CHECK (Status IN ('Draft', 'Posted', 'Cancelled'));
    ALTER TABLE dbo.PurchaseOrders ADD CONSTRAINT CK_PurchaseOrders_PostedAt CHECK ((Status = 'Posted' AND PostedAt IS NOT NULL) OR (Status <> 'Posted' AND PostedAt IS NULL));
    ALTER TABLE dbo.PurchaseOrderItems ADD CONSTRAINT CK_PurchaseOrderItems_Quantity CHECK (Quantity > 0);
    ALTER TABLE dbo.PurchaseOrderItems ADD CONSTRAINT CK_PurchaseOrderItems_UnitPrice CHECK (UnitPrice >= 0);
    ALTER TABLE dbo.SalesOrders ADD CONSTRAINT CK_SalesOrders_Status CHECK (Status IN ('Draft', 'Completed', 'Cancelled'));
    ALTER TABLE dbo.SalesOrders ADD CONSTRAINT CK_SalesOrders_CompletedAt CHECK ((Status = 'Completed' AND CompletedAt IS NOT NULL) OR (Status <> 'Completed' AND CompletedAt IS NULL));
    ALTER TABLE dbo.SalesOrderItems ADD CONSTRAINT CK_SalesOrderItems_Quantity CHECK (Quantity > 0);
    ALTER TABLE dbo.SalesOrderItems ADD CONSTRAINT CK_SalesOrderItems_UnitPrice CHECK (UnitPrice >= 0);
    ALTER TABLE dbo.SalesOrderItems ADD CONSTRAINT CK_SalesOrderItems_Discount CHECK (Discount >= 0 AND Discount <= Quantity * UnitPrice);
    ALTER TABLE dbo.InventoryTransactions ADD CONSTRAINT CK_InventoryTransactions_Source CHECK (
        (PurchaseOrderItemId IS NOT NULL AND SalesOrderItemId IS NULL AND Quantity > 0) OR
        (PurchaseOrderItemId IS NULL AND SalesOrderItemId IS NOT NULL AND Quantity < 0));
    ALTER TABLE dbo.ForecastRuns ADD CONSTRAINT CK_ForecastRuns_Status CHECK (Status IN ('Pending', 'Running', 'Completed', 'Failed'));
    ALTER TABLE dbo.ForecastRuns ADD CONSTRAINT CK_ForecastRuns_Dates CHECK (
        TrainingStartDate <= TrainingEndDate AND TrainingEndDate < ForecastStartDate AND ForecastStartDate <= ForecastEndDate);
    ALTER TABLE dbo.ForecastRuns ADD CONSTRAINT CK_ForecastRuns_MAE CHECK (MAE IS NULL OR MAE >= 0);
    ALTER TABLE dbo.ForecastRuns ADD CONSTRAINT CK_ForecastRuns_RMSE CHECK (RMSE IS NULL OR RMSE >= 0);
    ALTER TABLE dbo.ForecastResults ADD CONSTRAINT CK_ForecastResults_Quantity CHECK (ForecastQuantity >= 0);
    ALTER TABLE dbo.ForecastResults ADD CONSTRAINT CK_ForecastResults_Revenue CHECK (ForecastRevenue >= 0);
    ALTER TABLE dbo.ForecastResults ADD CONSTRAINT CK_ForecastResults_Bounds CHECK (
        (LowerBound IS NULL AND UpperBound IS NULL) OR
        (LowerBound IS NOT NULL AND UpperBound IS NOT NULL AND LowerBound >= 0 AND LowerBound <= ForecastQuantity AND ForecastQuantity <= UpperBound));
    ALTER TABLE dbo.ForecastModels ADD CONSTRAINT CK_ForecastModels_Parameters CHECK ([Parameters] IS NULL OR ISJSON([Parameters]) = 1);

    CREATE UNIQUE INDEX IX_Roles_Name ON dbo.Roles(Name);
    CREATE UNIQUE INDEX IX_Users_Email ON dbo.Users(Email);
    CREATE INDEX IX_Users_RoleId ON dbo.Users(RoleId);
    CREATE INDEX IX_Users_Status ON dbo.Users(Status);
    CREATE INDEX IX_LoginSessions_UserId_ExpiresAt ON dbo.LoginSessions(UserId, ExpiresAt);
    CREATE INDEX IX_Products_CategoryId ON dbo.Products(CategoryId);
    CREATE INDEX IX_Customers_PhoneNumber ON dbo.Customers(PhoneNumber);
    CREATE UNIQUE INDEX IX_Suppliers_TaxCode ON dbo.Suppliers(TaxCode) WHERE TaxCode IS NOT NULL;
    CREATE INDEX IX_PurchaseOrders_WarehouseId_OrderDate ON dbo.PurchaseOrders(WarehouseId, OrderDate);
    CREATE INDEX IX_PurchaseOrders_SupplierId ON dbo.PurchaseOrders(SupplierId);
    CREATE INDEX IX_PurchaseOrders_CreatedByUserId ON dbo.PurchaseOrders(CreatedByUserId);
    CREATE INDEX IX_PurchaseOrderItems_ProductId ON dbo.PurchaseOrderItems(ProductId);
    CREATE INDEX IX_SalesOrders_WarehouseId_OrderDate ON dbo.SalesOrders(WarehouseId, OrderDate) INCLUDE (Status);
    CREATE INDEX IX_SalesOrders_CustomerId ON dbo.SalesOrders(CustomerId);
    CREATE INDEX IX_SalesOrders_CreatedByUserId ON dbo.SalesOrders(CreatedByUserId);
    CREATE INDEX IX_SalesOrderItems_ProductId ON dbo.SalesOrderItems(ProductId);
    CREATE UNIQUE INDEX IX_InventoryTransactions_PurchaseOrderItemId
        ON dbo.InventoryTransactions(PurchaseOrderItemId) WHERE PurchaseOrderItemId IS NOT NULL;
    CREATE UNIQUE INDEX IX_InventoryTransactions_SalesOrderItemId
        ON dbo.InventoryTransactions(SalesOrderItemId) WHERE SalesOrderItemId IS NOT NULL;
    CREATE INDEX IX_InventoryTransactions_Warehouse_Product_Date
        ON dbo.InventoryTransactions(WarehouseId, ProductId, TransactionDate) INCLUDE (Quantity);
    CREATE INDEX IX_ForecastRuns_WarehouseId_CreatedAt ON dbo.ForecastRuns(WarehouseId, CreatedAt);
    CREATE INDEX IX_ForecastRuns_ForecastModelId ON dbo.ForecastRuns(ForecastModelId);
    CREATE INDEX IX_ForecastRuns_RequestedByUserId ON dbo.ForecastRuns(RequestedByUserId);
    CREATE INDEX IX_ForecastResults_ProductId_ForecastDate ON dbo.ForecastResults(ProductId, ForecastDate);

    INSERT dbo.SchemaVersions(Version) VALUES (3);
END;

COMMIT;
GO
