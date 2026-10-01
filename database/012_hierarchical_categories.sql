USE SalesForecastingDB;
GO
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = 9)
BEGIN
    IF COL_LENGTH(N'dbo.Categories', N'ParentCategoryId') IS NULL
        ALTER TABLE dbo.Categories ADD ParentCategoryId int NULL;

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID(N'dbo.Categories')
          AND name = N'FK_Categories_ParentCategory')
        ALTER TABLE dbo.Categories WITH CHECK
            ADD CONSTRAINT FK_Categories_ParentCategory
            FOREIGN KEY (ParentCategoryId) REFERENCES dbo.Categories(CategoryId);

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.Categories')
          AND name = N'IX_Categories_ParentCategoryId')
        CREATE INDEX IX_Categories_ParentCategoryId
            ON dbo.Categories(ParentCategoryId, Name);

    INSERT dbo.SchemaVersions(Version) VALUES (9);
END;

COMMIT;
GO
