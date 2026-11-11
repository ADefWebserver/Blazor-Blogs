IF NOT EXISTS (SELECT 1 FROM [dbo].[Settings] WHERE [SettingName] = N'Authentication:Google:Enabled')
BEGIN
    INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue])
    VALUES (N'Authentication:Google:Enabled', N'False')
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Settings] WHERE [SettingName] = N'Authentication:Google:ClientId')
BEGIN
    INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue])
    VALUES (N'Authentication:Google:ClientId', N'')
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Settings] WHERE [SettingName] = N'Authentication:Google:ClientSecret')
BEGIN
    INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue])
    VALUES (N'Authentication:Google:ClientSecret', N'')
END

UPDATE [dbo].[Settings]
SET [SettingValue] = '10.01.00'
WHERE [SettingName] = 'VersionNumber'
