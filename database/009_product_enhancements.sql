USE SalesForecastingDB;
GO
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = 6)
BEGIN
    IF COL_LENGTH(N'dbo.Products', N'Description') IS NULL
        ALTER TABLE dbo.Products ADD Description nvarchar(1000) NULL;

    IF COL_LENGTH(N'dbo.Products', N'ImageUrl') IS NULL
        ALTER TABLE dbo.Products ADD ImageUrl varchar(2048) NULL;

    IF COL_LENGTH(N'dbo.Products', N'MinimumStockLevel') IS NULL
        ALTER TABLE dbo.Products ADD MinimumStockLevel int NOT NULL
            CONSTRAINT DF_Products_MinimumStockLevel DEFAULT (0);

    IF COL_LENGTH(N'dbo.Products', N'UpdatedAt') IS NULL
        ALTER TABLE dbo.Products ADD UpdatedAt datetime2 NOT NULL
            CONSTRAINT DF_Products_UpdatedAt DEFAULT SYSUTCDATETIME();

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Products_MinimumStockLevel')
        EXEC(N'ALTER TABLE dbo.Products ADD CONSTRAINT CK_Products_MinimumStockLevel
            CHECK (MinimumStockLevel >= 0);');

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Products') AND name = N'IX_Products_IsActive_CategoryId')
        EXEC(N'CREATE INDEX IX_Products_IsActive_CategoryId
            ON dbo.Products(IsActive, CategoryId)
            INCLUDE (SKU, Name, SalePrice, MinimumStockLevel);');

    INSERT dbo.SchemaVersions(Version) VALUES (6);
END;

COMMIT;
GO
