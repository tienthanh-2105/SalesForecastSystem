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

CREATE PROCEDURE #CreateInventoryFixture
    @SaleQuantity int,
    @SalesOrderId bigint OUTPUT,
    @PurchaseOrderId bigint OUTPUT,
    @WarehouseId int OUTPUT,
    @ProductId int OUTPUT
AS
BEGIN
    DECLARE @UserId int,
            @CategoryId int,
            @SupplierId int,
            @Tag varchar(36) = CONVERT(varchar(36), NEWID());

    INSERT dbo.Users(RoleId, FullName, Email, PasswordHash, Status)
    SELECT RoleId, N'Rollback test', CONCAT(@Tag, '@example.invalid'), 'test-only-not-a-login', 'Locked'
    FROM dbo.Roles
    WHERE Name = 'Admin';
    SET @UserId = SCOPE_IDENTITY();

    INSERT dbo.Categories(Name) VALUES (@Tag);
    SET @CategoryId = SCOPE_IDENTITY();

    INSERT dbo.Products(CategoryId, SKU, Name, Unit, SalePrice)
    VALUES (@CategoryId, @Tag, N'Test product', N'Item', 10000);
    SET @ProductId = SCOPE_IDENTITY();

    INSERT dbo.Warehouses(Name) VALUES (@Tag);
    SET @WarehouseId = SCOPE_IDENTITY();

    INSERT dbo.Suppliers(Name) VALUES (@Tag);
    SET @SupplierId = SCOPE_IDENTITY();

    INSERT dbo.PurchaseOrders(
        OrderNumber, WarehouseId, SupplierId, CreatedByUserId, OrderDate)
    VALUES (@Tag, @WarehouseId, @SupplierId, @UserId, '20260901');
    SET @PurchaseOrderId = SCOPE_IDENTITY();

    INSERT dbo.PurchaseOrderItems(PurchaseOrderId, ProductId, Quantity, UnitPrice)
    VALUES (@PurchaseOrderId, @ProductId, 10, 6000);

    INSERT dbo.SalesOrders(OrderNumber, WarehouseId, CreatedByUserId, OrderDate)
    VALUES (@Tag, @WarehouseId, @UserId, '20260902');
    SET @SalesOrderId = SCOPE_IDENTITY();

    INSERT dbo.SalesOrderItems(SalesOrderId, ProductId, Quantity, UnitPrice, Discount)
    VALUES (@SalesOrderId, @ProductId, @SaleQuantity, 10000, 2000);
END;
GO

DECLARE @SalesOrder bigint,
        @PurchaseOrder bigint,
        @Warehouse int,
        @Product int;

BEGIN TRY
    BEGIN TRANSACTION;
    EXEC #CreateInventoryFixture 3, @SalesOrder OUTPUT, @PurchaseOrder OUTPUT, @Warehouse OUTPUT, @Product OUTPUT;
    EXEC dbo.usp_PostPurchaseOrder @PurchaseOrder;
    EXEC dbo.usp_PostPurchaseOrder @PurchaseOrder;
    EXEC dbo.usp_CompleteSalesOrder @SalesOrder;
    EXEC dbo.usp_CompleteSalesOrder @SalesOrder;

    IF (SELECT QuantityOnHand FROM dbo.vw_InventoryBalances
        WHERE WarehouseId = @Warehouse AND ProductId = @Product) <> 7
        THROW 51901, 'Incorrect inventory balance.', 1;

    IF (SELECT COUNT(*) FROM dbo.InventoryTransactions WHERE WarehouseId = @Warehouse) <> 2
        THROW 51902, 'Duplicate inventory transaction.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.vw_DailySales
        WHERE WarehouseId = @Warehouse AND QuantitySold = 3 AND Revenue = 28000)
        THROW 51903, 'Incorrect daily sales aggregate.', 1;

    ROLLBACK;
    PRINT 'PASS: atomic posting, inventory = 7, revenue = 28000, retries are idempotent.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    THROW;
END CATCH;

BEGIN TRY
    BEGIN TRANSACTION;
    EXEC #CreateInventoryFixture 11, @SalesOrder OUTPUT, @PurchaseOrder OUTPUT, @Warehouse OUTPUT, @Product OUTPUT;
    EXEC dbo.usp_PostPurchaseOrder @PurchaseOrder;
    EXEC dbo.usp_CompleteSalesOrder @SalesOrder;
    THROW 51904, 'Overselling was not rejected.', 1;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() <> 51107 THROW;
    PRINT 'PASS: overselling rejected and the transaction rolled back.';
END CATCH;

BEGIN TRY
    BEGIN TRANSACTION;
    EXEC #CreateInventoryFixture 3, @SalesOrder OUTPUT, @PurchaseOrder OUTPUT, @Warehouse OUTPUT, @Product OUTPUT;
    EXEC dbo.usp_PostPurchaseOrder @PurchaseOrder;
    EXEC dbo.usp_CompleteSalesOrder @SalesOrder;
    UPDATE dbo.SalesOrderItems SET Quantity = 2 WHERE SalesOrderId = @SalesOrder;
    THROW 51905, 'Completed sales order item was mutable.', 1;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() <> 51204 THROW;
    PRINT 'PASS: completed sales order items are immutable.';
END CATCH;

BEGIN TRY
    BEGIN TRANSACTION;
    EXEC #CreateInventoryFixture 3, @SalesOrder OUTPUT, @PurchaseOrder OUTPUT, @Warehouse OUTPUT, @Product OUTPUT;
    UPDATE dbo.SalesOrderItems SET Quantity = 0 WHERE SalesOrderId = @SalesOrder;
    THROW 51906, 'Zero quantity was not rejected.', 1;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() <> 547 THROW;
    PRINT 'PASS: non-positive quantity rejected by a database constraint.';
END CATCH;

BEGIN TRY
    BEGIN TRANSACTION;
    EXEC #CreateInventoryFixture 3, @SalesOrder OUTPUT, @PurchaseOrder OUTPUT, @Warehouse OUTPUT, @Product OUTPUT;
    UPDATE dbo.Products SET MinimumStockLevel = -1 WHERE ProductId = @Product;
    THROW 51909, 'Negative minimum stock level was not rejected.', 1;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() <> 547 THROW;
    PRINT 'PASS: negative product minimum stock rejected by a database constraint.';
END CATCH;

BEGIN TRY
    BEGIN TRANSACTION;
    EXEC #CreateInventoryFixture 3, @SalesOrder OUTPUT, @PurchaseOrder OUTPUT, @Warehouse OUTPUT, @Product OUTPUT;
    EXEC dbo.usp_PostPurchaseOrder @PurchaseOrder;
    UPDATE dbo.InventoryTransactions SET Quantity = 99 WHERE WarehouseId = @Warehouse;
    THROW 51907, 'Inventory transaction was mutable.', 1;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() <> 51205 THROW;
    PRINT 'PASS: inventory ledger is append-only.';
END CATCH;

BEGIN TRY
    BEGIN TRANSACTION;
    EXEC #CreateInventoryFixture 3, @SalesOrder OUTPUT, @PurchaseOrder OUTPUT, @Warehouse OUTPUT, @Product OUTPUT;
    INSERT dbo.ForecastModels(Name, Version) VALUES (CONVERT(nvarchar(36), NEWID()), 'test');
    DECLARE @ForecastModel int = SCOPE_IDENTITY(),
            @ForecastRun bigint;

    INSERT dbo.ForecastRuns(
        ForecastModelId, WarehouseId, TrainingStartDate, TrainingEndDate, ForecastStartDate, ForecastEndDate)
    VALUES (@ForecastModel, @Warehouse, '20260801', '20260831', '20260901', '20260907');
    SET @ForecastRun = SCOPE_IDENTITY();

    INSERT dbo.ForecastResults(
        ForecastRunId, ProductId, ForecastDate, ForecastQuantity, ForecastRevenue)
    VALUES (@ForecastRun, @Product, '20260908', 5, 50000);
    THROW 51908, 'Forecast outside the configured horizon was accepted.', 1;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() <> 51206 THROW;
    PRINT 'PASS: forecast result outside the run horizon rejected.';
END CATCH;

SELECT MAX(Version) AS SchemaVersion FROM dbo.SchemaVersions;
SELECT COUNT(*) AS TableCount FROM sys.tables;
SELECT name AS TableName FROM sys.tables ORDER BY name;
SELECT Name AS RoleName FROM dbo.Roles ORDER BY RoleId;
GO
