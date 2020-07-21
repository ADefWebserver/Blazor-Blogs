SET ANSI_NULLS ON
 
SET QUOTED_IDENTIFIER ON

UPDATE [dbo].[Settings]
   SET [SettingValue] = '01.00.90'
 WHERE [SettingName] = 'VersionNumber'