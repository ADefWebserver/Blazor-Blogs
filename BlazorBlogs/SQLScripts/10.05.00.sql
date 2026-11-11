IF NOT EXISTS (SELECT 1 FROM [dbo].[Settings] WHERE [SettingName] = N'ThemeAccentColor')
    INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue]) VALUES (N'ThemeAccentColor', N'#3B5BDB')

UPDATE [dbo].[Settings]
SET [SettingValue] = '10.05.00'
WHERE [SettingName] = 'VersionNumber'
