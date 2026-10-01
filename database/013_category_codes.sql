USE SalesForecastingDB;
GO
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = 10)
BEGIN
    IF COL_LENGTH(N'dbo.Categories', N'Code') IS NULL
    BEGIN
        ALTER TABLE dbo.Categories ADD Code varchar(20) NULL;

        EXEC(N'UPDATE dbo.Categories
            SET Code = CONCAT(''DM-'', FORMAT(CategoryId, ''0000''));');

        EXEC(N'ALTER TABLE dbo.Categories ALTER COLUMN Code varchar(20) NOT NULL;');
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.Categories')
          AND name = N'UX_Categories_Code')
        CREATE UNIQUE INDEX UX_Categories_Code ON dbo.Categories(Code);

    INSERT dbo.SchemaVersions(Version) VALUES (10);
END;

COMMIT;
GO
