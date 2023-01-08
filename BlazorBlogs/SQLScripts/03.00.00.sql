SET ANSI_NULLS ON
 
SET QUOTED_IDENTIFIER ON

UPDATE [dbo].[Settings]
   SET [SettingValue] = '03.00.00'
 WHERE [SettingName] = 'VersionNumber'