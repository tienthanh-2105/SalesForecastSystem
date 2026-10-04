USE SalesForecastingDB;
GO
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = 11)
BEGIN
    ALTER TABLE dbo.Users ADD CodeNumber bigint NULL;
    EXEC(N';WITH numbered AS (SELECT UserId, ROW_NUMBER() OVER (ORDER BY UserId) AS Number FROM dbo.Users) UPDATE u SET CodeNumber = n.Number FROM dbo.Users u JOIN numbered n ON n.UserId = u.UserId;');
    EXEC(N'ALTER TABLE dbo.Users ALTER COLUMN CodeNumber bigint NOT NULL;');
    DECLARE @next bigint = (SELECT COUNT_BIG(*) + 1 FROM dbo.Users);
    EXEC(N'CREATE SEQUENCE dbo.UserCodeSequence AS bigint START WITH ' + @next + N' INCREMENT BY 1 NO CYCLE;');
    EXEC(N'ALTER TABLE dbo.Users ADD CONSTRAINT DF_Users_CodeNumber DEFAULT (NEXT VALUE FOR dbo.UserCodeSequence) FOR CodeNumber;');
    EXEC(N'ALTER TABLE dbo.Users ADD Code AS (''ND-'' + CASE WHEN CodeNumber < 10000 THEN RIGHT(''0000'' + CONVERT(varchar(20), CodeNumber), 4) ELSE CONVERT(varchar(20), CodeNumber) END) PERSISTED;');
    EXEC(N'CREATE UNIQUE INDEX UX_Users_CodeNumber ON dbo.Users(CodeNumber);');
    INSERT dbo.SchemaVersions(Version) VALUES (11);
END;
COMMIT;
GO
