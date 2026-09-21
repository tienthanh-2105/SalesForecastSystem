USE SalesForecastingDB;
GO
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = 5)
BEGIN
    DECLARE @DropDefaults nvarchar(max);
    SELECT @DropDefaults = STRING_AGG(
        N'ALTER TABLE dbo.' + QUOTENAME(tableDefinition.name) +
        N' DROP CONSTRAINT ' + QUOTENAME(defaultDefinition.name), N';')
    FROM sys.default_constraints AS defaultDefinition
    JOIN sys.tables AS tableDefinition ON tableDefinition.object_id = defaultDefinition.parent_object_id
    JOIN sys.columns AS columnDefinition
      ON columnDefinition.object_id = defaultDefinition.parent_object_id
     AND columnDefinition.column_id = defaultDefinition.parent_column_id
    WHERE (tableDefinition.name = N'Users' AND columnDefinition.name = N'Status')
       OR (tableDefinition.name = N'PurchaseOrders' AND columnDefinition.name = N'Status')
       OR (tableDefinition.name = N'SalesOrders' AND columnDefinition.name = N'Status')
       OR (tableDefinition.name = N'ForecastRuns' AND columnDefinition.name = N'Status');

    IF @DropDefaults IS NOT NULL EXEC sp_executesql @DropDefaults;

    ALTER TABLE dbo.Users
        ADD CONSTRAINT DF_Users_Status DEFAULT 'Active' FOR Status;
    ALTER TABLE dbo.PurchaseOrders
        ADD CONSTRAINT DF_PurchaseOrders_Status DEFAULT 'Draft' FOR Status;
    ALTER TABLE dbo.SalesOrders
        ADD CONSTRAINT DF_SalesOrders_Status DEFAULT 'Draft' FOR Status;
    ALTER TABLE dbo.ForecastRuns
        ADD CONSTRAINT DF_ForecastRuns_Status DEFAULT 'Pending' FOR Status;

    INSERT dbo.SchemaVersions(Version) VALUES (5);
END;

COMMIT;
GO
