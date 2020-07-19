SET ANSI_NULLS ON
 
SET QUOTED_IDENTIFIER ON

IF  (0 = (SELECT count(*) FROM [dbo].[Settings] where [SettingName] = 'GoogleTrackingID'))
begin
    INSERT [dbo].[Settings] ([SettingName], [SettingValue]) VALUES (N'GoogleTrackingID', N'')
end;

UPDATE [dbo].[Settings]
   SET [SettingValue] = '01.00.80'
 WHERE [SettingName] = 'VersionNumber'