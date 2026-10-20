USE SalesForecastingDB;
GO
SET XACT_ABORT ON;
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

PRINT N'=== BẮT ĐẦU NẠP DỮ LIỆU CHUỖI THỜI GIAN PHỤC VỤ DỰ BÁO BÁN HÀNG (MACHINE LEARNING) ===';

-- 1. Tìm User, Warehouse, Supplier, Customer
DECLARE @UserId int;
SELECT TOP 1 @UserId = u.UserId 
FROM dbo.Users u
JOIN dbo.Roles r ON u.RoleId = r.RoleId
WHERE r.Name IN ('WarehouseManager', 'Admin') AND u.Status = 'Active';

IF @UserId IS NULL SELECT TOP 1 @UserId = UserId FROM dbo.Users WHERE Status = 'Active';

DECLARE @WarehouseId int;
SELECT TOP 1 @WarehouseId = WarehouseId FROM dbo.Warehouses WHERE WarehouseId = 1 AND IsActive = 1;
IF @WarehouseId IS NULL SELECT TOP 1 @WarehouseId = WarehouseId FROM dbo.Warehouses WHERE IsActive = 1;

DECLARE @SupplierId int;
SELECT TOP 1 @SupplierId = SupplierId FROM dbo.Suppliers WHERE IsActive = 1;

DECLARE @Customer1 int, @Customer2 int, @Customer3 int;
SELECT TOP 1 @Customer1 = CustomerId FROM dbo.Customers ORDER BY CustomerId;
SELECT TOP 1 @Customer2 = CustomerId FROM dbo.Customers WHERE CustomerId > @Customer1 ORDER BY CustomerId;
IF @Customer2 IS NULL SET @Customer2 = @Customer1;
SELECT TOP 1 @Customer3 = CustomerId FROM dbo.Customers WHERE CustomerId > @Customer2 ORDER BY CustomerId;
IF @Customer3 IS NULL SET @Customer3 = @Customer1;

-- Sản phẩm ProductId 2 (iPhone 15) và ProductId 20 (Pin Anker)
DECLARE @Prod1 int = 2, @Prod2 int = 20;
DECLARE @Price1 decimal(18,2) = 18990000, @Price2 decimal(18,2) = 950000;

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductId = @Prod1 AND IsActive = 1)
    SELECT TOP 1 @Prod1 = ProductId, @Price1 = SalePrice FROM dbo.Products WHERE IsActive = 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductId = @Prod2 AND IsActive = 1)
    SELECT TOP 1 @Prod2 = ProductId, @Price2 = SalePrice FROM dbo.Products WHERE IsActive = 1 AND ProductId <> @Prod1;

PRINT CONCAT(N'Kho hàng: ', @WarehouseId, N', Sản phẩm 1: ', @Prod1, N', Sản phẩm 2: ', @Prod2);

-- 2. Tạo Phiếu Nhập Hàng đầu kỳ (2026-08-01) để đảm bảo có đủ tồn kho
DECLARE @PoNumber varchar(50) = 'PO-INIT-20260801-01';
IF NOT EXISTS (SELECT 1 FROM dbo.PurchaseOrders WHERE OrderNumber = @PoNumber)
BEGIN
    BEGIN TRANSACTION;
    DECLARE @PoId bigint;
    INSERT INTO dbo.PurchaseOrders (OrderNumber, WarehouseId, SupplierId, CreatedByUserId, OrderDate, Status, Notes, CreatedAt)
    VALUES (@PoNumber, @WarehouseId, @SupplierId, @UserId, '2026-08-01', 'Draft', N'Phiếu nhập kho đầu kỳ nạp tồn kho chuỗi thời gian', '2026-08-01 08:00:00');
    SET @PoId = SCOPE_IDENTITY();

    DECLARE @PoItemId1 bigint, @PoItemId2 bigint;
    INSERT INTO dbo.PurchaseOrderItems (PurchaseOrderId, ProductId, Quantity, UnitPrice)
    VALUES (@PoId, @Prod1, 1000, 16000000);
    SET @PoItemId1 = SCOPE_IDENTITY();

    INSERT INTO dbo.PurchaseOrderItems (PurchaseOrderId, ProductId, Quantity, UnitPrice)
    VALUES (@PoId, @Prod2, 2000, 700000);
    SET @PoItemId2 = SCOPE_IDENTITY();

    UPDATE dbo.PurchaseOrders
    SET Status = 'Posted', PostedAt = '2026-08-01 08:30:00'
    WHERE PurchaseOrderId = @PoId;

    INSERT INTO dbo.InventoryTransactions (WarehouseId, ProductId, PurchaseOrderItemId, Quantity, TransactionDate, CreatedAt)
    VALUES 
    (@WarehouseId, @Prod1, @PoItemId1, 1000, '2026-08-01', '2026-08-01 08:30:00'),
    (@WarehouseId, @Prod2, @PoItemId2, 2000, '2026-08-01', '2026-08-01 08:30:00');

    COMMIT TRANSACTION;
    PRINT N'Đã nạp thành công Phiếu nhập hàng đầu kỳ 1000 đơn vị Prod1 và 2000 đơn vị Prod2.';
END;

-- 3. Tạo chuỗi đơn bán hàng hàng ngày từ 2026-08-01 đến 2026-10-18 (79 ngày)
DECLARE @StartDate date = '2026-08-01';
DECLARE @EndDate date = '2026-10-18';
DECLARE @CurrentDate date = @StartDate;
DECLARE @DayIndex int = 1;

WHILE @CurrentDate <= @EndDate
BEGIN
    DECLARE @SoNumber varchar(50) = CONCAT('SO-TS-', FORMAT(@CurrentDate, 'yyyyMMdd'), '-01');
    IF NOT EXISTS (SELECT 1 FROM dbo.SalesOrders WHERE OrderNumber = @SoNumber)
    BEGIN
        DECLARE @DayOfWeek int = DATEPART(dw, @CurrentDate); -- 1: Sunday, 7: Saturday
        DECLARE @CustId int = CASE @DayIndex % 3 WHEN 0 THEN @Customer1 WHEN 1 THEN @Customer2 ELSE @Customer3 END;

        -- Xác định sản lượng bán có tính chu kỳ 7 ngày (Cuối tuần bán cao hơn ngày thường)
        DECLARE @Qty1 int = 0;
        DECLARE @Qty2 int = 0;

        IF @DayOfWeek IN (1, 7) -- Cuối tuần: T7, CN
        BEGIN
            SET @Qty1 = 5 + (@DayIndex % 5);       -- 5 đến 9 cái
            SET @Qty2 = 12 + (@DayIndex % 8);      -- 12 đến 19 cái
        END
        ELSE IF @DayOfWeek = 6 -- Thứ 6
        BEGIN
            SET @Qty1 = 4 + (@DayIndex % 3);       -- 4 đến 6 cái
            SET @Qty2 = 8 + (@DayIndex % 5);       -- 8 đến 12 cái
        END
        ELSE -- Thứ 2 - Thứ 5
        BEGIN
            SET @Qty1 = 2 + (@DayIndex % 3);       -- 2 đến 4 cái
            SET @Qty2 = 4 + (@DayIndex % 4);       -- 4 đến 7 cái
        END;

        -- Giả lập một vài ngày không bán (NoSale) rải rác nhưng có tồn kho
        IF @DayIndex IN (15, 45, 62)
        BEGIN
            SET @Qty1 = 0;
        END;

        BEGIN TRANSACTION;

        DECLARE @SalesId bigint;

        INSERT INTO dbo.SalesOrders (OrderNumber, WarehouseId, CustomerId, CreatedByUserId, OrderDate, Status, ShippingAddress, Notes, CreatedAt)
        VALUES (@SoNumber, @WarehouseId, @CustId, @UserId, @CurrentDate, 'Draft', N'TP. Hồ Chí Minh', N'Đơn hàng phát sinh chuỗi thời gian', CAST(@CurrentDate AS datetime2));
        SET @SalesId = SCOPE_IDENTITY();

        IF @Qty1 > 0
        BEGIN
            DECLARE @ItemId1 bigint;
            INSERT INTO dbo.SalesOrderItems (SalesOrderId, ProductId, Quantity, UnitPrice, Discount)
            VALUES (@SalesId, @Prod1, @Qty1, @Price1, 0);
            SET @ItemId1 = SCOPE_IDENTITY();

            INSERT INTO dbo.InventoryTransactions (WarehouseId, ProductId, SalesOrderItemId, Quantity, TransactionDate, CreatedAt)
            VALUES (@WarehouseId, @Prod1, @ItemId1, -@Qty1, @CurrentDate, CAST(@CurrentDate AS datetime2));
        END;

        IF @Qty2 > 0
        BEGIN
            DECLARE @ItemId2 bigint;
            INSERT INTO dbo.SalesOrderItems (SalesOrderId, ProductId, Quantity, UnitPrice, Discount)
            VALUES (@SalesId, @Prod2, @Qty2, @Price2, 0);
            SET @ItemId2 = SCOPE_IDENTITY();

            INSERT INTO dbo.InventoryTransactions (WarehouseId, ProductId, SalesOrderItemId, Quantity, TransactionDate, CreatedAt)
            VALUES (@WarehouseId, @Prod2, @ItemId2, -@Qty2, @CurrentDate, CAST(@CurrentDate AS datetime2));
        END;

        UPDATE dbo.SalesOrders
        SET Status = 'Completed', CompletedAt = CAST(@CurrentDate AS datetime2)
        WHERE SalesOrderId = @SalesId;

        COMMIT TRANSACTION;
    END;

    SET @CurrentDate = DATEADD(day, 1, @CurrentDate);
    SET @DayIndex = @DayIndex + 1;
END;

PRINT N'=== HOÀN TẤT NẠP DỮ LIỆU CHUỖI THỜI GIAN BÁN HÀNG TỪ 2026-08-01 ĐẾN 2026-10-18 ===';
GO
