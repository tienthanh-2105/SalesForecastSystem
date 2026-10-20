USE SalesForecastingDB;
GO
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

PRINT N'=== BẮT ĐẦU NẠP DỮ LIỆU MẪU NHẬP HÀNG (PURCHASE ORDERS) ===';

-- 1. Đảm bảo có User phụ trách (WarehouseManager hoặc Admin)
DECLARE @UserId int;
SELECT TOP 1 @UserId = u.UserId 
FROM dbo.Users u
JOIN dbo.Roles r ON u.RoleId = r.RoleId
WHERE r.Name IN ('WarehouseManager', 'Admin') AND u.Status = 'Active';

IF @UserId IS NULL
BEGIN
    SELECT TOP 1 @UserId = UserId FROM dbo.Users WHERE Status = 'Active';
END;

IF @UserId IS NULL
BEGIN
    PRINT N'LỖI: Chưa có tài khoản người dùng hoạt động trong bảng Users để tạo phiếu nhập.';
    RETURN;
END;

-- 2. Đảm bảo có Kho hoạt động
DECLARE @WarehouseHcmId int, @WarehouseHnId int;
SELECT TOP 1 @WarehouseHcmId = WarehouseId FROM dbo.Warehouses WHERE Name LIKE N'%Hồ Chí Minh%' AND IsActive = 1;
IF @WarehouseHcmId IS NULL SELECT TOP 1 @WarehouseHcmId = WarehouseId FROM dbo.Warehouses WHERE IsActive = 1;

SELECT TOP 1 @WarehouseHnId = WarehouseId FROM dbo.Warehouses WHERE Name LIKE N'%Hà Nội%' AND IsActive = 1;
IF @WarehouseHnId IS NULL SELECT TOP 1 @WarehouseHnId = WarehouseId FROM dbo.Warehouses WHERE IsActive = 1 AND WarehouseId <> @WarehouseHcmId;
IF @WarehouseHnId IS NULL SET @WarehouseHnId = @WarehouseHcmId;

-- 3. Tạo Nhà Cung Cấp mẫu nếu chưa có
IF NOT EXISTS (SELECT 1 FROM dbo.Suppliers WHERE TaxCode = '0101258670')
    INSERT INTO dbo.Suppliers (Name, TaxCode, PhoneNumber, Email, Address, IsActive)
    VALUES (N'Công ty Cổ phần Bán lẻ Kỹ thuật số FPT (Synnex FPT)', '0101258670', '02473006666', 'contact@synnexfpt.com.vn', N'Tòa nhà FPT Cầu Giấy, Phố Duy Tân, Cầu Giấy, Hà Nội', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Suppliers WHERE TaxCode = '0302888749')
    INSERT INTO dbo.Suppliers (Name, TaxCode, PhoneNumber, Email, Address, IsActive)
    VALUES (N'Công ty Cổ phần Thế Giới Số (Digiworld Corporation)', '0302888749', '02839290059', 'info@digiworld.com.vn', N'Tầng 8, Tòa nhà Nam Á, 201-203 Cách Mạng Tháng Tám, Quận 3, TP. Hồ Chí Minh', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Suppliers WHERE TaxCode = '0304998695')
    INSERT INTO dbo.Suppliers (Name, TaxCode, PhoneNumber, Email, Address, IsActive)
    VALUES (N'Công ty CP Dịch vụ Phân phối Tổng hợp Dầu khí (PSD)', '0304998695', '02839115578', 'psd@petrosetco.com.vn', N'Lầu 6, PetroVietnam Tower, 1-5 Lê Duẩn, Quận 1, TP. Hồ Chí Minh', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Suppliers WHERE TaxCode = '0301389824')
    INSERT INTO dbo.Suppliers (Name, TaxCode, PhoneNumber, Email, Address, IsActive)
    VALUES (N'Công ty TNHH MTV Công Nghệ Tin Học Viễn Sơn', '0301389824', '02838326085', 'info@microstar.vn', N'162 Bùi Thị Xuân, Phường Phạm Ngũ Lão, Quận 1, TP. Hồ Chí Minh', 1);

DECLARE @SupplierFptId int, @SupplierDgwId int, @SupplierPsdId int, @SupplierVsId int;
SELECT @SupplierFptId = SupplierId FROM dbo.Suppliers WHERE TaxCode = '0101258670';
SELECT @SupplierDgwId = SupplierId FROM dbo.Suppliers WHERE TaxCode = '0302888749';
SELECT @SupplierPsdId = SupplierId FROM dbo.Suppliers WHERE TaxCode = '0304998695';
SELECT @SupplierVsId = SupplierId FROM dbo.Suppliers WHERE TaxCode = '0301389824';

-- Helper Table để chứa danh sách chi tiết các phiếu cần nạp
DECLARE @OrdersToCreate TABLE (
    OrderNumber varchar(50),
    WarehouseId int,
    SupplierId int,
    OrderDate date,
    TargetStatus varchar(20),
    Notes nvarchar(500)
);

INSERT INTO @OrdersToCreate (OrderNumber, WarehouseId, SupplierId, OrderDate, TargetStatus, Notes) VALUES
('PO-20261001-HCM01', @WarehouseHcmId, @SupplierFptId, '2026-10-01', 'Posted', N'Nhập kho đợt 1 - Điện thoại Apple chính hãng cho kho TP. Hồ Chí Minh'),
('PO-20261002-HCM02', @WarehouseHcmId, @SupplierDgwId, '2026-10-02', 'Posted', N'Nhập đợt 1 - Dòng máy tính xách tay HP và Lenovo cho kho TP. Hồ Chí Minh'),
('PO-20261003-HN01',   @WarehouseHnId,  @SupplierFptId, '2026-10-03', 'Posted', N'Nhập hàng đầu kỳ cho kho Hà Nội: Điện thoại và laptop'),
('PO-20261005-HCM03', @WarehouseHcmId, @SupplierPsdId, '2026-10-05', 'Posted', N'Nhập kho phụ kiện sạc và ốp lưng chính hãng cho kho TP. Hồ Chí Minh'),
('PO-20261006-HN02',   @WarehouseHnId,  @SupplierVsId,  '2026-10-06', 'Posted', N'Nhập thiết bị ngoại vi và tai nghe cho kho Hà Nội'),
('PO-20261015-HCM04', @WarehouseHcmId, @SupplierFptId, '2026-10-15', 'Draft',  N'Phiếu nháp: Nhập bổ sung đợt giữa tháng 10, đang chờ kiểm đếm hàng'),
('PO-20261016-HN03',   @WarehouseHnId,  @SupplierDgwId, '2026-10-16', 'Cancelled', N'Phiếu hủy: Nhà cung cấp báo đứt hàng model Lenovo');

-- Helper Table cho các Items
DECLARE @OrderItemsToCreate TABLE (
    OrderNumber varchar(50),
    Sku varchar(50),
    Quantity int,
    UnitPrice decimal(18,2)
);

INSERT INTO @OrderItemsToCreate (OrderNumber, Sku, Quantity, UnitPrice) VALUES
('PO-20261001-HCM01', 'TGDD-PHN-IP17-256', 25, 20500000),
('PO-20261001-HCM01', 'TGDD-PHN-IP15-128', 20, 16200000),
('PO-20261001-HCM01', 'XLA-IP17-256',      10, 22800000),

('PO-20261002-HCM02', 'TGDD-LAP-HP245G10',      15, 16000000),
('PO-20261002-HCM02', 'TGDD-LAP-LNV-IP3-15ARP10', 12, 18000000),
('PO-20261002-HCM02', 'LAP-HP14AMD',            10, 10500000),

('PO-20261003-HN01', 'TGDD-PHN-IP17-256',        20, 20500000),
('PO-20261003-HN01', 'TGDD-LAP-HP245G10',        10, 16000000),
('PO-20261003-HN01', 'TGDD-LAP-LNV-IP3-15ARP10',  8, 18000000),

('PO-20261005-HCM03', 'TGDD-PWB-XM-JP313',      40, 450000),
('PO-20261005-HCM03', 'TGDD-PWB-XM-JP365',      30, 850000),
('PO-20261005-HCM03', 'TGDD-CAB-BAS-CB000135',  60, 120000),
('PO-20261005-HCM03', 'TGDD-CAB-ANK-A8060',     50, 110000),
('PO-20261005-HCM03', 'TGDD-PCS-APL-IP17P-TW',  30, 950000),

('PO-20261006-HN02', 'TGDD-MOU-LOG-M170',    50, 135000),
('PO-20261006-HN02', 'TGDD-KEY-LOG-K120',    40, 115000),
('PO-20261006-HN02', 'TGDD-KEY-LOG-MK250',   30, 420000),
('PO-20261006-HN02', 'TGDD-HBT-SON-WFC510',  35, 550000),

('PO-20261015-HCM04', 'TGDD-PHN-IP17-256',    15, 20400000),
('PO-20261015-HCM04', 'TGDD-PWB-XM-JP365',    20, 840000),
('PO-20261015-HCM04', 'TGDD-CAB-ANK-A8060',   30, 105000),

('PO-20261016-HN03', 'TGDD-LAP-LNV-IP3-15ARP10', 10, 17900000);

-- Vòng lặp nạp từng phiếu
DECLARE @CurOrderNumber varchar(50), @CurWarehouseId int, @CurSupplierId int, @CurOrderDate date, @CurTargetStatus varchar(20), @CurNotes nvarchar(500);
DECLARE @NewPOId bigint;

DECLARE po_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT OrderNumber, WarehouseId, SupplierId, OrderDate, TargetStatus, Notes FROM @OrdersToCreate;

OPEN po_cursor;
FETCH NEXT FROM po_cursor INTO @CurOrderNumber, @CurWarehouseId, @CurSupplierId, @CurOrderDate, @CurTargetStatus, @CurNotes;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.PurchaseOrders WHERE OrderNumber = @CurOrderNumber)
    BEGIN
        INSERT INTO dbo.PurchaseOrders (OrderNumber, WarehouseId, SupplierId, CreatedByUserId, OrderDate, Status, Notes)
        VALUES (@CurOrderNumber, @CurWarehouseId, @CurSupplierId, @UserId, @CurOrderDate, 'Draft', @CurNotes);

        SET @NewPOId = SCOPE_IDENTITY();

        -- Nạp các chi tiết dòng hàng
        INSERT INTO dbo.PurchaseOrderItems (PurchaseOrderId, ProductId, Quantity, UnitPrice)
        SELECT @NewPOId, p.ProductId, i.Quantity, i.UnitPrice
        FROM @OrderItemsToCreate i
        JOIN dbo.Products p ON UPPER(RTRIM(LTRIM(p.SKU))) = UPPER(RTRIM(LTRIM(i.Sku)))
        WHERE i.OrderNumber = @CurOrderNumber;

        IF @CurTargetStatus = 'Posted'
        BEGIN
            IF EXISTS (SELECT 1 FROM dbo.PurchaseOrderItems WHERE PurchaseOrderId = @NewPOId)
            BEGIN
                EXEC dbo.usp_PostPurchaseOrder @PurchaseOrderId = @NewPOId;
                PRINT CONCAT(N'-> Đã tạo và xác nhận nhập kho thành công: ', @CurOrderNumber);
            END
            ELSE
            BEGIN
                PRINT CONCAT(N'-> Bỏ qua xác nhận ', @CurOrderNumber, N' do các SKU trong phiếu chưa được tạo trong bảng Products.');
            END;
        END
        ELSE IF @CurTargetStatus = 'Cancelled'
        BEGIN
            UPDATE dbo.PurchaseOrders SET Status = 'Cancelled' WHERE PurchaseOrderId = @NewPOId;
            PRINT CONCAT(N'-> Đã tạo và hủy phiếu: ', @CurOrderNumber);
        END
        ELSE
        BEGIN
            PRINT CONCAT(N'-> Đã tạo phiếu ở trạng thái Nháp: ', @CurOrderNumber);
        END;
    END
    ELSE
    BEGIN
        PRINT CONCAT(N'-> Phiếu nhập đã tồn tại, bỏ qua: ', @CurOrderNumber);
    END;

    FETCH NEXT FROM po_cursor INTO @CurOrderNumber, @CurWarehouseId, @CurSupplierId, @CurOrderDate, @CurTargetStatus, @CurNotes;
END;

CLOSE po_cursor;
DEALLOCATE po_cursor;

PRINT N'=== HOÀN TẤT NẠP DỮ LIỆU NHẬP HÀNG ===';
GO
