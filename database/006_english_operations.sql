USE SalesForecastingDB;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER VIEW dbo.vw_InventoryBalances AS
SELECT WarehouseId, ProductId, SUM(CONVERT(bigint, Quantity)) AS QuantityOnHand
FROM dbo.InventoryTransactions
GROUP BY WarehouseId, ProductId;
GO

CREATE OR ALTER VIEW dbo.vw_DailySales AS
SELECT salesOrder.WarehouseId,
       item.ProductId,
       salesOrder.OrderDate,
       SUM(CONVERT(bigint, item.Quantity)) AS QuantitySold,
       SUM(item.LineTotal) AS Revenue,
       COUNT_BIG(DISTINCT salesOrder.SalesOrderId) AS OrderCount
FROM dbo.SalesOrders AS salesOrder
JOIN dbo.SalesOrderItems AS item ON item.SalesOrderId = salesOrder.SalesOrderId
WHERE salesOrder.Status = 'Completed'
GROUP BY salesOrder.WarehouseId, item.ProductId, salesOrder.OrderDate;
GO

CREATE OR ALTER VIEW dbo.vw_SalesOrderTotals AS
SELECT salesOrder.SalesOrderId,
       salesOrder.OrderNumber,
       salesOrder.Status,
       COALESCE(SUM(item.LineTotal), 0) AS TotalAmount
FROM dbo.SalesOrders AS salesOrder
LEFT JOIN dbo.SalesOrderItems AS item ON item.SalesOrderId = salesOrder.SalesOrderId
GROUP BY salesOrder.SalesOrderId, salesOrder.OrderNumber, salesOrder.Status;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PostPurchaseOrder
    @PurchaseOrderId bigint
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @WarehouseId int,
                @OrderDate date,
                @Status varchar(20),
                @LockResult int,
                @LockResource nvarchar(255);

        SELECT @WarehouseId = WarehouseId,
               @OrderDate = OrderDate,
               @Status = Status
        FROM dbo.PurchaseOrders WITH (UPDLOCK, HOLDLOCK)
        WHERE PurchaseOrderId = @PurchaseOrderId;

        IF @WarehouseId IS NULL
            THROW 51001, N'Purchase order was not found.', 1;

        IF @Status = 'Posted'
        BEGIN
            COMMIT;
            RETURN;
        END;

        IF @Status <> 'Draft'
            THROW 51002, N'Only draft purchase orders can be posted.', 1;

        SET @LockResource = CONCAT(N'Inventory:Warehouse:', @WarehouseId);
        EXEC @LockResult = sys.sp_getapplock
            @Resource = @LockResource,
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @LockResult < 0
            THROW 51003, N'Warehouse inventory is being updated. Try again.', 1;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.Warehouses
            WHERE WarehouseId = @WarehouseId AND IsActive = 1)
            THROW 51004, N'Warehouse is inactive.', 1;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.PurchaseOrderItems WITH (UPDLOCK, HOLDLOCK)
            WHERE PurchaseOrderId = @PurchaseOrderId)
            THROW 51005, N'Purchase order has no items.', 1;

        IF EXISTS (
            SELECT 1
            FROM dbo.PurchaseOrderItems AS item
            JOIN dbo.Products AS product ON product.ProductId = item.ProductId
            WHERE item.PurchaseOrderId = @PurchaseOrderId AND product.IsActive = 0)
            THROW 51006, N'Purchase order contains an inactive product.', 1;

        UPDATE dbo.PurchaseOrders
        SET Status = 'Posted', PostedAt = SYSUTCDATETIME()
        WHERE PurchaseOrderId = @PurchaseOrderId;

        INSERT dbo.InventoryTransactions(
            WarehouseId,
            ProductId,
            PurchaseOrderItemId,
            Quantity,
            TransactionDate)
        SELECT @WarehouseId,
               ProductId,
               PurchaseOrderItemId,
               Quantity,
               @OrderDate
        FROM dbo.PurchaseOrderItems
        WHERE PurchaseOrderId = @PurchaseOrderId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CompleteSalesOrder
    @SalesOrderId bigint
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @WarehouseId int,
                @OrderDate date,
                @Status varchar(20),
                @LockResult int,
                @LockResource nvarchar(255);

        SELECT @WarehouseId = WarehouseId,
               @OrderDate = OrderDate,
               @Status = Status
        FROM dbo.SalesOrders WITH (UPDLOCK, HOLDLOCK)
        WHERE SalesOrderId = @SalesOrderId;

        IF @WarehouseId IS NULL
            THROW 51101, N'Sales order was not found.', 1;

        IF @Status = 'Completed'
        BEGIN
            COMMIT;
            RETURN;
        END;

        IF @Status <> 'Draft'
            THROW 51102, N'Only draft sales orders can be completed.', 1;

        SET @LockResource = CONCAT(N'Inventory:Warehouse:', @WarehouseId);
        EXEC @LockResult = sys.sp_getapplock
            @Resource = @LockResource,
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @LockResult < 0
            THROW 51103, N'Warehouse inventory is being updated. Try again.', 1;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.Warehouses
            WHERE WarehouseId = @WarehouseId AND IsActive = 1)
            THROW 51104, N'Warehouse is inactive.', 1;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.SalesOrderItems WITH (UPDLOCK, HOLDLOCK)
            WHERE SalesOrderId = @SalesOrderId)
            THROW 51105, N'Sales order has no items.', 1;

        IF EXISTS (
            SELECT 1
            FROM dbo.SalesOrderItems AS item
            JOIN dbo.Products AS product ON product.ProductId = item.ProductId
            WHERE item.SalesOrderId = @SalesOrderId AND product.IsActive = 0)
            THROW 51106, N'Sales order contains an inactive product.', 1;

        IF EXISTS (
            SELECT 1
            FROM dbo.SalesOrderItems AS item
            LEFT JOIN dbo.vw_InventoryBalances AS balance
                ON balance.WarehouseId = @WarehouseId
               AND balance.ProductId = item.ProductId
            WHERE item.SalesOrderId = @SalesOrderId
              AND item.Quantity > COALESCE(balance.QuantityOnHand, 0))
            THROW 51107, N'Insufficient inventory to complete the sales order.', 1;

        UPDATE dbo.SalesOrders
        SET Status = 'Completed', CompletedAt = SYSUTCDATETIME()
        WHERE SalesOrderId = @SalesOrderId;

        INSERT dbo.InventoryTransactions(
            WarehouseId,
            ProductId,
            SalesOrderItemId,
            Quantity,
            TransactionDate)
        SELECT @WarehouseId,
               ProductId,
               SalesOrderItemId,
               -Quantity,
               @OrderDate
        FROM dbo.SalesOrderItems
        WHERE SalesOrderId = @SalesOrderId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_PurchaseOrders_ProtectPosted
ON dbo.PurchaseOrders
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted WHERE Status <> 'Draft')
        THROW 51201, N'Posted or cancelled purchase orders are immutable.', 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_SalesOrders_ProtectCompleted
ON dbo.SalesOrders
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted WHERE Status <> 'Draft')
        THROW 51202, N'Completed or cancelled sales orders are immutable.', 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_PurchaseOrderItems_ProtectPosted
ON dbo.PurchaseOrderItems
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM dbo.PurchaseOrders AS purchaseOrder WITH (UPDLOCK, HOLDLOCK)
        JOIN (
            SELECT PurchaseOrderId FROM inserted
            UNION
            SELECT PurchaseOrderId FROM deleted) AS affected
          ON affected.PurchaseOrderId = purchaseOrder.PurchaseOrderId
        WHERE purchaseOrder.Status <> 'Draft')
        THROW 51203, N'Only draft purchase order items can be changed.', 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_SalesOrderItems_ProtectCompleted
ON dbo.SalesOrderItems
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM dbo.SalesOrders AS salesOrder WITH (UPDLOCK, HOLDLOCK)
        JOIN (
            SELECT SalesOrderId FROM inserted
            UNION
            SELECT SalesOrderId FROM deleted) AS affected
          ON affected.SalesOrderId = salesOrder.SalesOrderId
        WHERE salesOrder.Status <> 'Draft')
        THROW 51204, N'Only draft sales order items can be changed.', 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_InventoryTransactions_AppendOnly
ON dbo.InventoryTransactions
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted)
        THROW 51205, N'Inventory transactions are append-only.', 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_InventoryTransactions_NonNegativeBalance
ON dbo.InventoryTransactions
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM (
            SELECT transactionRow.WarehouseId,
                   transactionRow.ProductId,
                   SUM(CONVERT(bigint, transactionRow.Quantity)) AS QuantityOnHand
            FROM dbo.InventoryTransactions AS transactionRow WITH (UPDLOCK, HOLDLOCK)
            JOIN (
                SELECT DISTINCT WarehouseId, ProductId
                FROM inserted) AS affected
              ON affected.WarehouseId = transactionRow.WarehouseId
             AND affected.ProductId = transactionRow.ProductId
            GROUP BY transactionRow.WarehouseId, transactionRow.ProductId) AS balance
        WHERE balance.QuantityOnHand < 0)
        THROW 51210, N'Inventory balance cannot be negative.', 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_ForecastResults_ValidateDate
ON dbo.ForecastResults
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted AS result
        JOIN dbo.ForecastRuns AS run ON run.ForecastRunId = result.ForecastRunId
        WHERE result.ForecastDate < run.ForecastStartDate
           OR result.ForecastDate > run.ForecastEndDate)
        THROW 51206, N'Forecast result date must be inside the run horizon.', 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_ForecastRuns_ProtectResultDates
ON dbo.ForecastRuns
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted AS run
        JOIN dbo.ForecastResults AS result ON result.ForecastRunId = run.ForecastRunId
        WHERE result.ForecastDate < run.ForecastStartDate
           OR result.ForecastDate > run.ForecastEndDate)
        THROW 51209, N'Forecast horizon cannot exclude stored results.', 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_PurchaseOrders_RequireDraftOnInsert
ON dbo.PurchaseOrders
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted WHERE Status <> 'Draft')
        THROW 51207, N'New purchase orders must be drafts.', 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_SalesOrders_RequireDraftOnInsert
ON dbo.SalesOrders
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted WHERE Status <> 'Draft')
        THROW 51208, N'New sales orders must be drafts.', 1;
END;
GO

IF DATABASE_PRINCIPAL_ID(N'SalesForecastApp') IS NULL
    CREATE ROLE SalesForecastApp;
GO

GRANT SELECT ON SCHEMA::dbo TO SalesForecastApp;
GRANT INSERT, UPDATE, DELETE ON dbo.Categories TO SalesForecastApp;
GRANT INSERT, UPDATE, DELETE ON dbo.Products TO SalesForecastApp;
GRANT INSERT, UPDATE, DELETE ON dbo.Warehouses TO SalesForecastApp;
GRANT INSERT, UPDATE, DELETE ON dbo.Customers TO SalesForecastApp;
GRANT INSERT, UPDATE, DELETE ON dbo.Suppliers TO SalesForecastApp;
GRANT INSERT, DELETE ON dbo.PurchaseOrders TO SalesForecastApp;
GRANT UPDATE ON dbo.PurchaseOrders (
    OrderNumber, WarehouseId, SupplierId, CreatedByUserId, OrderDate, Notes) TO SalesForecastApp;
GRANT INSERT, DELETE ON dbo.SalesOrders TO SalesForecastApp;
GRANT UPDATE ON dbo.SalesOrders (
    OrderNumber, WarehouseId, CustomerId, CreatedByUserId, OrderDate, Notes) TO SalesForecastApp;
GRANT INSERT, UPDATE, DELETE ON dbo.PurchaseOrderItems TO SalesForecastApp;
GRANT INSERT, UPDATE, DELETE ON dbo.SalesOrderItems TO SalesForecastApp;
GRANT INSERT, UPDATE ON dbo.ForecastModels TO SalesForecastApp;
GRANT INSERT, UPDATE ON dbo.ForecastRuns TO SalesForecastApp;
GRANT INSERT, UPDATE, DELETE ON dbo.ForecastResults TO SalesForecastApp;
DENY INSERT, UPDATE, DELETE ON dbo.InventoryTransactions TO SalesForecastApp;
GRANT EXECUTE ON dbo.usp_PostPurchaseOrder TO SalesForecastApp;
GRANT EXECUTE ON dbo.usp_CompleteSalesOrder TO SalesForecastApp;
GO
