SET ANSI_NULLS ON
 
SET QUOTED_IDENTIFIER ON

UPDATE [dbo].[Settings]
   SET [SettingValue] = '01.01.10'
 WHERE [SettingName] = 'VersionNumber'