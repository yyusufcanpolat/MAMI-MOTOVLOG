-- V5.3 read-only preflight: choose your own MAMI database; no DDL or writes.
SELECT DB_NAME() AS DatabaseName;
DECLARE @required TABLE (TableName sysname, ColumnName sysname);
INSERT INTO @required VALUES
 (N'Settings',N'HeroTitle'),(N'Settings',N'HeroDescription'),(N'Settings',N'HeroImage'),(N'Settings',N'AboutText'),
 (N'Messages',N'Phone'),(N'AdminAccounts',N'PasswordHash'),(N'Videos',N'YouTubeUrl'),(N'Rides',N'Title'),(N'Albums',N'RideId'),(N'AlbumPhotos',N'AlbumId'),(N'Gear',N'ExternalUrl'),
 (N'AspNetUsers',N'EmailConfirmed'),(N'AspNetUsers',N'SecurityStamp'),(N'BikeBrands',N'Name'),(N'BikeModels',N'BrandId'),(N'BikeVariants',N'ModelId'),
 (N'RiderBikes',N'OwnerId'),(N'RiderBikes',N'AverageFuelL100'),(N'RiderFuels',N'FullTank'),(N'RiderFuels',N'AtKm'),
 (N'RiderMaintenances',N'NextDate'),(N'RiderMaintenances',N'NextKm'),(N'RiderExpenses',N'AmountTry'),(N'RiderEquipments',N'PhotoFileName'),
 (N'RiderTrips',N'GeoJson'),(N'RiderTrips',N'IsApproved'),(N'RiderReminders',N'LastEmailedUtc'),(N'RiderFavorites',N'TripId'),(N'PlatformEntries',N'UpdatedUtc'),(N'AdminAudits',N'Operation');
SELECT TableName, ColumnName FROM @required WHERE COL_LENGTH(N'dbo.' + TableName, ColumnName) IS NULL;
IF EXISTS (SELECT 1 FROM @required WHERE COL_LENGTH(N'dbo.' + TableName, ColumnName) IS NULL)
 THROW 51053, 'Missing schema. Check V5.2 additive migration and selected database.', 1;
PRINT 'V5.3 required column preflight OK. This does not replace SQL Server integration tests.';
