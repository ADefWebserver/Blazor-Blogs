SET ANSI_NULLS ON
 
SET QUOTED_IDENTIFIER ON

UPDATE [dbo].[Settings]
   SET [SettingValue] = '02.00.00'
 WHERE [SettingName] = 'VersionNumber'