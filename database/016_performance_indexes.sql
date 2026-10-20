USE SalesForecastingDB;
GO
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = 13)
BEGIN
    -- 1. Index on SalesOrders for high-performance reporting & time-series analysis
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SalesOrders') AND name = N'IX_SalesOrders_Status_OrderDate')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_SalesOrders_Status_OrderDate
        ON dbo.SalesOrders(Status, OrderDate)
        INCLUDE (WarehouseId, CustomerId, CreatedByUserId, OrderNumber);
    END;

    -- 2. Index on SalesOrderItems for order detail aggregations & report joins
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SalesOrderItems') AND name = N'IX_SalesOrderItems_SalesOrderId_Covering')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_SalesOrderItems_SalesOrderId_Covering
        ON dbo.SalesOrderItems(SalesOrderId)
        INCLUDE (ProductId, Quantity, UnitPrice, Discount, LineTotal);
    END;

    -- 3. Composite covering index on InventoryTransactions for FIFO costing & ledger history lookups
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InventoryTransactions') AND name = N'IX_InventoryTransactions_FIFO_Ledger')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_InventoryTransactions_FIFO_Ledger
        ON dbo.InventoryTransactions(ProductId, WarehouseId, InventoryTransactionId)
        INCLUDE (Quantity, SalesOrderItemId, PurchaseOrderItemId, TransactionDate);
    END;

    -- 4. Index on PurchaseOrders for status and date range reports
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PurchaseOrders') AND name = N'IX_PurchaseOrders_Status_OrderDate')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_PurchaseOrders_Status_OrderDate
        ON dbo.PurchaseOrders(Status, OrderDate)
        INCLUDE (WarehouseId, SupplierId, CreatedByUserId, OrderNumber);
    END;

    -- 5. Index on PurchaseOrderItems for purchase detail joins
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PurchaseOrderItems') AND name = N'IX_PurchaseOrderItems_PurchaseOrderId_Covering')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_PurchaseOrderItems_PurchaseOrderId_Covering
        ON dbo.PurchaseOrderItems(PurchaseOrderId)
        INCLUDE (ProductId, Quantity, UnitPrice, LineTotal);
    END;

    INSERT dbo.SchemaVersions(Version) VALUES (13);
END;
COMMIT;
GO
