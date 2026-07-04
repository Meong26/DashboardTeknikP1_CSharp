USE [DB_Teknik_P1];
GO

-- 1. Buat Tabel tbl_Settings
CREATE TABLE [dbo].[tbl_Settings] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [SettingKey] VARCHAR(100) NOT NULL UNIQUE,
    [SettingValue] VARCHAR(255) NOT NULL,
    [Description] VARCHAR(255) NULL,
    [LastUpdated] DATETIME DEFAULT GETDATE()
);
GO

-- 2. Insert Data Default
INSERT INTO [dbo].[tbl_Settings] ([SettingKey], [SettingValue], [Description])
VALUES 
('TargetDowntime', '1.5', 'Target maksimum persentase downtime per area (default: 1.5%)'),
('TvModeDuration', '10000', 'Durasi transisi slide pada TV Dashboard Mode dalam milidetik (default: 10000ms = 10 detik)');
GO
