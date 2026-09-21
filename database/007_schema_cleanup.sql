USE SalesForecastingDB;
GO
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = 4)
BEGIN
    IF COL_LENGTH(N'dbo.ForecastModels', N'ThamSo') IS NOT NULL
    BEGIN
        DECLARE @DropForecastModelChecks nvarchar(max);
        SELECT @DropForecastModelChecks = STRING_AGG(
            N'ALTER TABLE dbo.ForecastModels DROP CONSTRAINT ' + QUOTENAME(name), N';')
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.ForecastModels');

        IF @DropForecastModelChecks IS NOT NULL
            EXEC sp_executesql @DropForecastModelChecks;

        EXEC sp_rename N'dbo.ForecastModels.ThamSo', N'Parameters', N'COLUMN';
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.ForecastModels')
          AND name = N'CK_ForecastModels_Parameters')
    BEGIN
        EXEC sp_executesql N'
            ALTER TABLE dbo.ForecastModels
            ADD CONSTRAINT CK_ForecastModels_Parameters
            CHECK ([Parameters] IS NULL OR ISJSON([Parameters]) = 1);';
    END;

    INSERT dbo.SchemaVersions(Version) VALUES (4);
END;

COMMIT;
GO
