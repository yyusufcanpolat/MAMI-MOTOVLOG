-- Additive upgrade AFTER V52_UPDATE_SQLSERVER.sql. Back up first.
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID(N'dbo.Settings',N'U') IS NULL OR OBJECT_ID(N'dbo.RiderFuels',N'U') IS NULL OR OBJECT_ID(N'dbo.PlatformEntries',N'U') IS NULL
  THROW 51054, 'Wrong database or V5.2 upgrade missing.', 1;
 IF COL_LENGTH(N'dbo.Settings',N'MotorcycleTitle') IS NULL ALTER TABLE dbo.Settings ADD MotorcycleTitle nvarchar(100) NOT NULL DEFAULT N'';
 IF COL_LENGTH(N'dbo.Settings',N'MotorcycleText') IS NULL ALTER TABLE dbo.Settings ADD MotorcycleText nvarchar(4000) NOT NULL DEFAULT N'';
 IF COL_LENGTH(N'dbo.Settings',N'GuideTitle') IS NULL ALTER TABLE dbo.Settings ADD GuideTitle nvarchar(100) NOT NULL DEFAULT N'';
 IF COL_LENGTH(N'dbo.Settings',N'GuideText') IS NULL ALTER TABLE dbo.Settings ADD GuideText nvarchar(max) NOT NULL DEFAULT N'';
 IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RiderFuels') AND name=N'IX_RiderFuels_BikeId_AtKm')
 BEGIN
  IF EXISTS(SELECT 1 FROM dbo.RiderFuels GROUP BY BikeId,AtKm HAVING COUNT(*)>1)
   PRINT 'WARNING: legacy duplicate odometers; index skipped, no rows removed.';
  ELSE CREATE UNIQUE INDEX IX_RiderFuels_BikeId_AtKm ON dbo.RiderFuels(BikeId,AtKm);
 END;
 COMMIT;
 PRINT 'MAMI V5.4 update OK; existing data preserved.';
END TRY
BEGIN CATCH
 IF @@TRANCOUNT > 0 ROLLBACK;
 THROW;
END CATCH;
