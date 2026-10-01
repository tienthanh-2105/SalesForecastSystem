USE SalesForecastingDB;
GO
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = 7)
BEGIN
    IF COL_LENGTH(N'dbo.Customers', N'IsActive') IS NULL
        ALTER TABLE dbo.Customers ADD IsActive bit NOT NULL
            CONSTRAINT DF_Customers_IsActive DEFAULT (1);

    IF EXISTS (
        SELECT Email FROM dbo.Customers WHERE Email IS NOT NULL
        GROUP BY Email HAVING COUNT(*) > 1)
        THROW 51010, N'Customer emails must be deduplicated before applying version 7.', 1;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.Customers') AND name = N'IX_Customers_Email')
        CREATE UNIQUE INDEX IX_Customers_Email ON dbo.Customers(Email) WHERE Email IS NOT NULL;

    INSERT dbo.SchemaVersions(Version) VALUES (7);
END;

COMMIT;
GO
