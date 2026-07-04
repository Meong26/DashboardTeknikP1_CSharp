USE [DB_Teknik_P1];
GO

-- Tambahkan kolom IsActive jika belum ada (1 = Aktif, 0 = Nonaktif)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Users]') AND name = 'IsActive')
BEGIN
    ALTER TABLE [dbo].[tbl_Users]
    ADD [IsActive] BIT NOT NULL DEFAULT 1;
END
GO
