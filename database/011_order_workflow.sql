USE SalesForecastingDB;
GO
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

BEGIN TRANSACTION;
IF COL_LENGTH(N'dbo.SalesOrders', N'CustomerName') IS NULL
    ALTER TABLE dbo.SalesOrders ADD CustomerName nvarchar(100) NULL;
IF COL_LENGTH(N'dbo.SalesOrders', N'CustomerPhone') IS NULL
    ALTER TABLE dbo.SalesOrders ADD CustomerPhone varchar(15) NULL;
IF COL_LENGTH(N'dbo.SalesOrders', N'ShippingAddress') IS NULL
    ALTER TABLE dbo.SalesOrders ADD ShippingAddress nvarchar(255) NULL;

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.SalesOrders') AND name = N'CK_SalesOrders_Status')
    ALTER TABLE dbo.SalesOrders DROP CONSTRAINT CK_SalesOrders_Status;
ALTER TABLE dbo.SalesOrders WITH CHECK ADD CONSTRAINT CK_SalesOrders_Status
    CHECK (Status IN ('Draft', 'Pending', 'Delivering', 'Completed', 'Cancelled'));

GRANT UPDATE ON dbo.PurchaseOrders (Status) TO SalesForecastApp;
GRANT UPDATE ON dbo.SalesOrders (Status) TO SalesForecastApp;
GRANT UPDATE ON dbo.SalesOrders (CustomerName, CustomerPhone, ShippingAddress) TO SalesForecastApp;
COMMIT;
GO

-- Reapply the completion procedure after 006_english_operations.sql on every deployment.
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

        IF @Status NOT IN ('Draft', 'Delivering')
            THROW 51102, N'Only draft or delivering sales orders can be completed.', 1;

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

CREATE OR ALTER TRIGGER dbo.TR_SalesOrders_ProtectCompleted
ON dbo.SalesOrders
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted WHERE Status IN ('Completed', 'Cancelled'))
        THROW 51202, N'Completed or cancelled sales orders are immutable.', 1;
    IF EXISTS (
        SELECT 1 FROM inserted AS i
        JOIN deleted AS d ON d.SalesOrderId = i.SalesOrderId
        WHERE (d.Status = 'Draft' AND i.Status NOT IN ('Draft', 'Pending', 'Completed', 'Cancelled'))
           OR (d.Status = 'Pending' AND i.Status NOT IN ('Delivering', 'Cancelled'))
           OR (d.Status = 'Delivering' AND i.Status NOT IN ('Completed', 'Cancelled'))
           OR (d.Status <> 'Draft' AND (
                i.OrderNumber <> d.OrderNumber OR i.WarehouseId <> d.WarehouseId
                OR ISNULL(i.CustomerId, -1) <> ISNULL(d.CustomerId, -1)
                OR i.CreatedByUserId <> d.CreatedByUserId OR i.OrderDate <> d.OrderDate
                OR ISNULL(i.Notes, N'') <> ISNULL(d.Notes, N'')
                OR ISNULL(i.CustomerName, N'') <> ISNULL(d.CustomerName, N'')
                OR ISNULL(i.CustomerPhone, '') <> ISNULL(d.CustomerPhone, '')
                OR ISNULL(i.ShippingAddress, N'') <> ISNULL(d.ShippingAddress, N''))))
        THROW 51211, N'Invalid sales order transition or header modification.', 1;
    IF EXISTS (
        SELECT 1 FROM inserted AS i
        JOIN deleted AS d ON d.SalesOrderId = i.SalesOrderId
        WHERE d.Status = 'Draft' AND i.Status = 'Pending'
          AND NOT EXISTS (SELECT 1 FROM dbo.SalesOrderItems WITH (UPDLOCK, HOLDLOCK)
                          WHERE SalesOrderId = i.SalesOrderId))
        THROW 51212, N'Sales order has no items.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = 8)
    INSERT dbo.SchemaVersions(Version) VALUES (8);
GO

