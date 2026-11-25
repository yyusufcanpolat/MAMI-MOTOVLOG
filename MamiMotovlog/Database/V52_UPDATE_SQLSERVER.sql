-- MAMI V5.2: additive only. Select your existing MAMI database in SSMS; BACK UP first.
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID(N'dbo.RiderBikes',N'U') IS NULL OR OBJECT_ID(N'dbo.Settings',N'U') IS NULL
   THROW 51000, 'Wrong database or V5.1 schema missing.', 1;
 IF OBJECT_ID(N'dbo.PlatformEntries',N'U') IS NULL
 CREATE TABLE dbo.PlatformEntries ([Key] nvarchar(200) NOT NULL PRIMARY KEY, [Value] nvarchar(2000) NOT NULL, UpdatedUtc datetime2 NOT NULL);
 IF OBJECT_ID(N'dbo.AdminAudits',N'U') IS NULL
 CREATE TABLE dbo.AdminAudits (Id int IDENTITY(1,1) NOT NULL PRIMARY KEY, Operation nvarchar(250) NOT NULL, CreatedUtc datetime2 NOT NULL);
 IF COL_LENGTH(N'dbo.Settings',N'HeroTitle') IS NULL ALTER TABLE dbo.Settings ADD HeroTitle nvarchar(100) NOT NULL DEFAULT N'';
 IF COL_LENGTH(N'dbo.Settings',N'HeroDescription') IS NULL ALTER TABLE dbo.Settings ADD HeroDescription nvarchar(550) NOT NULL DEFAULT N'';
 IF COL_LENGTH(N'dbo.Settings',N'HeroImage') IS NULL ALTER TABLE dbo.Settings ADD HeroImage nvarchar(260) NOT NULL DEFAULT N'';
 IF COL_LENGTH(N'dbo.Settings',N'AboutText') IS NULL ALTER TABLE dbo.Settings ADD AboutText nvarchar(2000) NOT NULL DEFAULT N'';
 COMMIT;
 PRINT 'MAMI V5.2 update OK. Existing data preserved.';
END TRY
BEGIN CATCH
 IF @@TRANCOUNT > 0 ROLLBACK;
 THROW;
END CATCH;
