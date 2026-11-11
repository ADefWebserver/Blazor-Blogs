UPDATE [dbo].[AspNetUsers]
SET [NewsletterSubscriber] = 0
WHERE [NewsletterSubscriber] IS NULL

IF COL_LENGTH('dbo.AspNetUsers', 'NewsletterToken') IS NULL
    ALTER TABLE [dbo].[AspNetUsers] ADD [NewsletterToken] [nvarchar](64) NULL

EXEC('UPDATE [dbo].[AspNetUsers]
SET [NewsletterToken] = LOWER(REPLACE(CONVERT(varchar(36), NEWID()), ''-'', '''') + REPLACE(CONVERT(varchar(36), NEWID()), ''-'', ''''))
WHERE [NewsletterToken] IS NULL')

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUsers_NewsletterToken' AND object_id = OBJECT_ID(N'dbo.AspNetUsers'))
    EXEC('CREATE UNIQUE INDEX [IX_AspNetUsers_NewsletterToken] ON [dbo].[AspNetUsers]([NewsletterToken]) WHERE [NewsletterToken] IS NOT NULL')

UPDATE [dbo].[Settings]
SET [SettingValue] = '10.03.00'
WHERE [SettingName] = 'VersionNumber'
