/* MAMI V3 / SQL Server 2019 / ADDITIVE ONLY
   BEFORE: complete backup of the current database. Use a NEW EMPTY V4 TEST database only; NEVER run on the V2 live database.
   Does not DROP, DELETE, ALTER existing V2 tables or change V2 admin account.
   Run once via SQL Server Management Studio / Plesk SQL query as a user with CREATE TABLE rights.
   Target: database configured in ConnectionStrings:SqlServer (e.g. mamimoto_mami_v4test).
*/
SET XACT_ABORT ON;
BEGIN TRY
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
CREATE TABLE dbo.AspNetUsers (
  Id nvarchar(450) NOT NULL CONSTRAINT PK_AspNetUsers PRIMARY KEY,
  UserName nvarchar(256) NULL, NormalizedUserName nvarchar(256) NULL,
  Email nvarchar(256) NULL, NormalizedEmail nvarchar(256) NULL,
  EmailConfirmed bit NOT NULL, PasswordHash nvarchar(max) NULL,
  SecurityStamp nvarchar(max) NULL, ConcurrencyStamp nvarchar(max) NULL,
  PhoneNumber nvarchar(max) NULL, PhoneNumberConfirmed bit NOT NULL,
  TwoFactorEnabled bit NOT NULL, LockoutEnd datetimeoffset NULL,
  LockoutEnabled bit NOT NULL, AccessFailedCount int NOT NULL,
  DisplayName nvarchar(75) NOT NULL, JoinedUtc datetime2 NOT NULL
);
IF OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NULL
CREATE TABLE dbo.AspNetRoles (
  Id nvarchar(450) NOT NULL CONSTRAINT PK_AspNetRoles PRIMARY KEY,
  Name nvarchar(256) NULL, NormalizedName nvarchar(256) NULL, ConcurrencyStamp nvarchar(max) NULL
);
IF OBJECT_ID(N'dbo.AspNetRoleClaims', N'U') IS NULL
CREATE TABLE dbo.AspNetRoleClaims (
  Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY,
  RoleId nvarchar(450) NOT NULL, ClaimType nvarchar(max) NULL, ClaimValue nvarchar(max) NULL,
  CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY(RoleId) REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE
);
IF OBJECT_ID(N'dbo.AspNetUserClaims', N'U') IS NULL
CREATE TABLE dbo.AspNetUserClaims (
  Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetUserClaims PRIMARY KEY,
  UserId nvarchar(450) NOT NULL, ClaimType nvarchar(max) NULL, ClaimValue nvarchar(max) NULL,
  CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY(UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
);
IF OBJECT_ID(N'dbo.AspNetUserLogins', N'U') IS NULL
CREATE TABLE dbo.AspNetUserLogins (
  LoginProvider nvarchar(128) NOT NULL, ProviderKey nvarchar(128) NOT NULL,
  ProviderDisplayName nvarchar(max) NULL, UserId nvarchar(450) NOT NULL,
  CONSTRAINT PK_AspNetUserLogins PRIMARY KEY(LoginProvider, ProviderKey),
  CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY(UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
);
IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NULL
CREATE TABLE dbo.AspNetUserRoles (
  UserId nvarchar(450) NOT NULL, RoleId nvarchar(450) NOT NULL,
  CONSTRAINT PK_AspNetUserRoles PRIMARY KEY(UserId,RoleId),
  CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY(UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
  CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY(RoleId) REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE
);
IF OBJECT_ID(N'dbo.AspNetUserTokens', N'U') IS NULL
CREATE TABLE dbo.AspNetUserTokens (
  UserId nvarchar(450) NOT NULL, LoginProvider nvarchar(128) NOT NULL,
  Name nvarchar(128) NOT NULL, Value nvarchar(max) NULL,
  CONSTRAINT PK_AspNetUserTokens PRIMARY KEY(UserId,LoginProvider,Name),
  CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY(UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'EmailIndex' AND object_id=OBJECT_ID(N'dbo.AspNetUsers'))
CREATE INDEX EmailIndex ON dbo.AspNetUsers(NormalizedEmail);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UserNameIndex' AND object_id=OBJECT_ID(N'dbo.AspNetUsers'))
CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'RoleNameIndex' AND object_id=OBJECT_ID(N'dbo.AspNetRoles'))
CREATE UNIQUE INDEX RoleNameIndex ON dbo.AspNetRoles(NormalizedName) WHERE NormalizedName IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetRoleClaims_RoleId' AND object_id=OBJECT_ID(N'dbo.AspNetRoleClaims'))
CREATE INDEX IX_AspNetRoleClaims_RoleId ON dbo.AspNetRoleClaims(RoleId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetUserClaims_UserId' AND object_id=OBJECT_ID(N'dbo.AspNetUserClaims'))
CREATE INDEX IX_AspNetUserClaims_UserId ON dbo.AspNetUserClaims(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetUserLogins_UserId' AND object_id=OBJECT_ID(N'dbo.AspNetUserLogins'))
CREATE INDEX IX_AspNetUserLogins_UserId ON dbo.AspNetUserLogins(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetUserRoles_RoleId' AND object_id=OBJECT_ID(N'dbo.AspNetUserRoles'))
CREATE INDEX IX_AspNetUserRoles_RoleId ON dbo.AspNetUserRoles(RoleId);

IF OBJECT_ID(N'dbo.BikeBrands', N'U') IS NULL
CREATE TABLE dbo.BikeBrands (Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_BikeBrands PRIMARY KEY, Name nvarchar(75) NOT NULL);
IF OBJECT_ID(N'dbo.BikeModels', N'U') IS NULL
CREATE TABLE dbo.BikeModels (
  Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_BikeModels PRIMARY KEY, BrandId int NOT NULL, Name nvarchar(100) NOT NULL,
  CONSTRAINT FK_BikeModels_BikeBrands_BrandId FOREIGN KEY(BrandId) REFERENCES dbo.BikeBrands(Id) ON DELETE CASCADE
);
IF OBJECT_ID(N'dbo.BikeVariants', N'U') IS NULL
CREATE TABLE dbo.BikeVariants (
  Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_BikeVariants PRIMARY KEY,
  ModelId int NOT NULL, [Year] int NOT NULL, Edition nvarchar(85) NOT NULL,
  EngineCc int NULL, PowerHp decimal(8,2) NULL, TorqueNm decimal(8,2) NULL, TankLitres decimal(8,2) NULL,
  CONSTRAINT FK_BikeVariants_BikeModels_ModelId FOREIGN KEY(ModelId) REFERENCES dbo.BikeModels(Id) ON DELETE CASCADE
);
IF OBJECT_ID(N'dbo.RiderBikes', N'U') IS NULL
CREATE TABLE dbo.RiderBikes (
  Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RiderBikes PRIMARY KEY,
  OwnerId nvarchar(450) NOT NULL, VariantId int NULL,
  BrandName nvarchar(90) NOT NULL, ModelName nvarchar(110) NOT NULL,
  ModelYear int NOT NULL, Edition nvarchar(100) NOT NULL, OdometerKm int NOT NULL,
  AverageFuelL100 decimal(8,2) NULL, TankLitres decimal(8,2) NULL,
  Notes nvarchar(1000) NOT NULL, CreatedUtc datetime2 NOT NULL,
  CONSTRAINT FK_RiderBikes_AspNetUsers_OwnerId FOREIGN KEY(OwnerId) REFERENCES dbo.AspNetUsers(Id),
  CONSTRAINT FK_RiderBikes_BikeVariants_VariantId FOREIGN KEY(VariantId) REFERENCES dbo.BikeVariants(Id) ON DELETE SET NULL
);
IF OBJECT_ID(N'dbo.RiderMaintenances', N'U') IS NULL
CREATE TABLE dbo.RiderMaintenances (
  Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RiderMaintenances PRIMARY KEY,
  BikeId int NOT NULL, Category nvarchar(90) NOT NULL, Description nvarchar(300) NOT NULL,
  ServicedOn datetime2 NOT NULL, AtKm int NOT NULL, CostTry decimal(12,2) NOT NULL,
  NextDate datetime2 NULL, NextKm int NULL, AttachmentFileName nvarchar(90) NOT NULL,
  CONSTRAINT FK_RiderMaintenances_RiderBikes_BikeId FOREIGN KEY(BikeId) REFERENCES dbo.RiderBikes(Id) ON DELETE CASCADE
);
IF OBJECT_ID(N'dbo.RiderFuels', N'U') IS NULL
CREATE TABLE dbo.RiderFuels (
  Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RiderFuels PRIMARY KEY,
  BikeId int NOT NULL, FuelledOn datetime2 NOT NULL, AtKm int NOT NULL,
  Litres decimal(10,2) NOT NULL, TotalTry decimal(12,2) NOT NULL, FullTank bit NOT NULL,
  CONSTRAINT FK_RiderFuels_RiderBikes_BikeId FOREIGN KEY(BikeId) REFERENCES dbo.RiderBikes(Id) ON DELETE CASCADE
);
IF OBJECT_ID(N'dbo.RiderExpenses', N'U') IS NULL
CREATE TABLE dbo.RiderExpenses (
  Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RiderExpenses PRIMARY KEY,
  BikeId int NOT NULL, Category nvarchar(40) NOT NULL, Description nvarchar(220) NOT NULL,
  SpentOn datetime2 NOT NULL, AmountTry decimal(12,2) NOT NULL,
  CONSTRAINT FK_RiderExpenses_RiderBikes_BikeId FOREIGN KEY(BikeId) REFERENCES dbo.RiderBikes(Id) ON DELETE CASCADE
);
IF OBJECT_ID(N'dbo.RiderEquipments', N'U') IS NULL
CREATE TABLE dbo.RiderEquipments (
  Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RiderEquipments PRIMARY KEY,
  OwnerId nvarchar(450) NOT NULL, Category nvarchar(50) NOT NULL, Brand nvarchar(120) NOT NULL,
  Model nvarchar(120) NOT NULL, PurchasedOn datetime2 NULL, WarrantyEndsOn datetime2 NULL, CostTry decimal(12,2) NULL, PhotoFileName nvarchar(90) NOT NULL
);
IF OBJECT_ID(N'dbo.RiderTrips', N'U') IS NULL
CREATE TABLE dbo.RiderTrips (
  Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RiderTrips PRIMARY KEY,
  OwnerId nvarchar(450) NOT NULL, BikeId int NULL,
  Title nvarchar(140) NOT NULL, StartLabel nvarchar(120) NOT NULL, EndLabel nvarchar(120) NOT NULL,
  StartLat float NOT NULL, StartLon float NOT NULL, EndLat float NOT NULL, EndLon float NOT NULL,
  DistanceKm decimal(11,2) NOT NULL, DurationMinutes int NOT NULL,
  CreatedUtc datetime2 NOT NULL, CompletedOn datetime2 NULL,
  ActualSpendTry decimal(12,2) NULL, Journal nvarchar(2000) NOT NULL,
  GeoJson nvarchar(max) NOT NULL, IsPublic bit NOT NULL, IsApproved bit NOT NULL,
  CONSTRAINT FK_RiderTrips_RiderBikes_BikeId FOREIGN KEY(BikeId) REFERENCES dbo.RiderBikes(Id) ON DELETE SET NULL
);
IF OBJECT_ID(N'dbo.RiderReminders', N'U') IS NULL
CREATE TABLE dbo.RiderReminders (
  Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RiderReminders PRIMARY KEY,
  OwnerId nvarchar(450) NOT NULL, BikeId int NOT NULL, Kind nvarchar(65) NOT NULL,
  Title nvarchar(180) NOT NULL, DueOn datetime2 NOT NULL, LastEmailedUtc datetime2 NULL
);
IF OBJECT_ID(N'dbo.RiderFavorites', N'U') IS NULL
CREATE TABLE dbo.RiderFavorites (
  Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RiderFavorites PRIMARY KEY,
  OwnerId nvarchar(450) NOT NULL, TripId int NOT NULL, CreatedUtc datetime2 NOT NULL,
  CONSTRAINT FK_RiderFavorites_RiderTrips_TripId FOREIGN KEY(TripId) REFERENCES dbo.RiderTrips(Id) ON DELETE CASCADE
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RiderFavorites_OwnerId_TripId' AND object_id=OBJECT_ID(N'dbo.RiderFavorites'))
CREATE UNIQUE INDEX IX_RiderFavorites_OwnerId_TripId ON dbo.RiderFavorites(OwnerId,TripId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RiderFavorites_TripId' AND object_id=OBJECT_ID(N'dbo.RiderFavorites'))
CREATE INDEX IX_RiderFavorites_TripId ON dbo.RiderFavorites(TripId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_BikeBrands_Name' AND object_id=OBJECT_ID(N'dbo.BikeBrands'))
CREATE UNIQUE INDEX IX_BikeBrands_Name ON dbo.BikeBrands(Name);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_BikeModels_BrandId_Name' AND object_id=OBJECT_ID(N'dbo.BikeModels'))
CREATE UNIQUE INDEX IX_BikeModels_BrandId_Name ON dbo.BikeModels(BrandId,Name);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_BikeVariants_ModelId_Year_Edition' AND object_id=OBJECT_ID(N'dbo.BikeVariants'))
CREATE UNIQUE INDEX IX_BikeVariants_ModelId_Year_Edition ON dbo.BikeVariants(ModelId,[Year],Edition);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RiderBikes_OwnerId' AND object_id=OBJECT_ID(N'dbo.RiderBikes'))
CREATE INDEX IX_RiderBikes_OwnerId ON dbo.RiderBikes(OwnerId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RiderBikes_VariantId' AND object_id=OBJECT_ID(N'dbo.RiderBikes'))
CREATE INDEX IX_RiderBikes_VariantId ON dbo.RiderBikes(VariantId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RiderMaintenances_BikeId' AND object_id=OBJECT_ID(N'dbo.RiderMaintenances'))
CREATE INDEX IX_RiderMaintenances_BikeId ON dbo.RiderMaintenances(BikeId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RiderFuels_BikeId' AND object_id=OBJECT_ID(N'dbo.RiderFuels'))
CREATE INDEX IX_RiderFuels_BikeId ON dbo.RiderFuels(BikeId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RiderExpenses_BikeId' AND object_id=OBJECT_ID(N'dbo.RiderExpenses'))
CREATE INDEX IX_RiderExpenses_BikeId ON dbo.RiderExpenses(BikeId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RiderEquipments_OwnerId' AND object_id=OBJECT_ID(N'dbo.RiderEquipments'))
CREATE INDEX IX_RiderEquipments_OwnerId ON dbo.RiderEquipments(OwnerId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RiderTrips_OwnerId' AND object_id=OBJECT_ID(N'dbo.RiderTrips'))
CREATE INDEX IX_RiderTrips_OwnerId ON dbo.RiderTrips(OwnerId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RiderTrips_BikeId' AND object_id=OBJECT_ID(N'dbo.RiderTrips'))
CREATE INDEX IX_RiderTrips_BikeId ON dbo.RiderTrips(BikeId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RiderReminders_OwnerId' AND object_id=OBJECT_ID(N'dbo.RiderReminders'))
CREATE INDEX IX_RiderReminders_OwnerId ON dbo.RiderReminders(OwnerId);
COMMIT TRANSACTION;
PRINT 'MAMI V3 install OK: Existing V2 data not changed.';
END TRY
BEGIN CATCH
  IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
  THROW;
END CATCH;
