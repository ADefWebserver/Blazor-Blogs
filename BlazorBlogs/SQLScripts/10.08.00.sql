IF OBJECT_ID(N'dbo.ThemePackages', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ThemePackages] (
        [ThemePackageId] uniqueidentifier NOT NULL
            CONSTRAINT [PK_ThemePackages] PRIMARY KEY,
        [Name] nvarchar(100) NOT NULL,
        [Slug] nvarchar(50) NOT NULL,
        [Version] nvarchar(32) NOT NULL,
        [ManifestJson] nvarchar(max) NOT NULL,
        [StylesheetUrl] nvarchar(500) NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [UQ_ThemePackages_Package] UNIQUE ([Slug], [Version])
    )
END

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[Settings]
    WHERE [SettingName] = N'ActiveThemePackageId')
    INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue])
    VALUES (N'ActiveThemePackageId', N'')

UPDATE [dbo].[Settings]
SET [SettingValue] = '10.08.00'
WHERE [SettingName] = 'VersionNumber'
