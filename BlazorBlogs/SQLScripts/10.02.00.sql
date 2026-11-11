IF NOT EXISTS (SELECT 1 FROM [dbo].[Settings] WHERE [SettingName] = N'ApplicationHeaderEnabled')
BEGIN
    INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue])
    VALUES (N'ApplicationHeaderEnabled', N'True')
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Settings] WHERE [SettingName] = N'ApplicationHeaderEditorMode')
BEGIN
    INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue])
    SELECT N'ApplicationHeaderEditorMode',
           CASE
               WHEN NULLIF(LTRIM(RTRIM(COALESCE([SettingValue], N''))), N'') IS NULL THEN N'Rich'
               ELSE N'Raw'
           END
    FROM [dbo].[Settings]
    WHERE [SettingName] = N'ApplicationHeader'

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue])
        VALUES (N'ApplicationHeaderEditorMode', N'Rich')
    END
END

UPDATE [dbo].[Settings]
SET [SettingValue] = '10.02.00'
WHERE [SettingName] = 'VersionNumber'
