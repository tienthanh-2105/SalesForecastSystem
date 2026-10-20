USE SalesForecastingDB;
GO
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = 12)
BEGIN
    ALTER TABLE dbo.ForecastRuns ADD ModelDiagnostics nvarchar(max) NULL;
    EXEC(N'ALTER TABLE dbo.ForecastRuns ADD CONSTRAINT CK_ForecastRuns_ModelDiagnostics CHECK (ModelDiagnostics IS NULL OR ISJSON(ModelDiagnostics) = 1);');
    INSERT dbo.SchemaVersions(Version) VALUES (12);
END;
COMMIT;
GO
