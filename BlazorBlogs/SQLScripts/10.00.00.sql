IF NOT EXISTS (SELECT 1 FROM [dbo].[Settings] WHERE [SettingName] = N'SiteBaseUrl')
BEGIN
    INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue])
    VALUES (N'SiteBaseUrl', N'')
END

UPDATE [dbo].[Settings]
SET [SettingValue] = '10.00.00'
WHERE [SettingName] = 'VersionNumber'
