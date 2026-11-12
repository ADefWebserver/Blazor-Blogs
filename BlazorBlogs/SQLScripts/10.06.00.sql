IF COL_LENGTH('dbo.Files', 'Description') IS NULL
    ALTER TABLE [dbo].[Files] ADD [Description] [nvarchar](4000) NULL

UPDATE [dbo].[Settings]
SET [SettingValue] = '10.06.00'
WHERE [SettingName] = 'VersionNumber'
