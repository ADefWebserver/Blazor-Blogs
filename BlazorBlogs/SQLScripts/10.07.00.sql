IF NOT EXISTS (
    SELECT 1 FROM [dbo].[Settings]
    WHERE [SettingName] = N'GoogleSiteSearchEnabled')
    INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue])
    VALUES (N'GoogleSiteSearchEnabled', N'False')

UPDATE [dbo].[Settings]
SET [SettingValue] = '10.07.00'
WHERE [SettingName] = 'VersionNumber'
