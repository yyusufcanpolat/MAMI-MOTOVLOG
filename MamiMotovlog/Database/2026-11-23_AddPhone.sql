-- SQL Server 2019: MAMI MOTOVLOG Phone alanı (mevcut verileri silmez)
-- Çalıştırmadan önce veritabanının yedeğini alın.
IF OBJECT_ID(N'dbo.Messages', N'U') IS NOT NULL
AND COL_LENGTH(N'dbo.Messages', N'Phone') IS NULL
BEGIN
    ALTER TABLE dbo.Messages ADD Phone nvarchar(11) NOT NULL
    CONSTRAINT DF_Messages_Phone DEFAULT N'';
END;
GO
