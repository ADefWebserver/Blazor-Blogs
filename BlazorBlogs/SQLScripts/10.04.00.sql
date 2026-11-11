IF COL_LENGTH('dbo.Files', 'Price') IS NULL
    ALTER TABLE [dbo].[Files] ADD [Price] [decimal](10,2) NOT NULL CONSTRAINT [DF_Files_Price] DEFAULT (0)

IF COL_LENGTH('dbo.Files', 'OriginalFileName') IS NULL
    ALTER TABLE [dbo].[Files] ADD [OriginalFileName] [nvarchar](260) NULL

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FilePurchases]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[FilePurchases](
        [Id] [int] IDENTITY(1,1) NOT NULL CONSTRAINT [PK_FilePurchases] PRIMARY KEY,
        [FileId] [int] NOT NULL CONSTRAINT [FK_FilePurchases_Files] REFERENCES [dbo].[Files]([FileId]),
        [UserId] [nvarchar](450) NULL,
        [BuyerEmail] [nvarchar](256) NULL,
        [Status] [nvarchar](20) NOT NULL,
        [Amount] [decimal](10,2) NOT NULL,
        [TaxAmount] [decimal](10,2) NOT NULL CONSTRAINT [DF_FilePurchases_Tax] DEFAULT (0),
        [Currency] [nvarchar](3) NOT NULL,
        [StripeSessionId] [nvarchar](255) NOT NULL CONSTRAINT [UQ_FilePurchases_Session] UNIQUE,
        [StripePaymentIntentId] [nvarchar](255) NULL,
        [LiveMode] [bit] NOT NULL,
        [DownloadToken] [nvarchar](64) NULL,
        [TokenExpiresUtc] [datetime] NULL,
        [TokenDownloadCount] [int] NOT NULL CONSTRAINT [DF_FilePurchases_TokenCount] DEFAULT (0),
        [LastLinkSentUtc] [datetime] NULL,
        [CreatedUtc] [datetime] NOT NULL,
        [PaidUtc] [datetime] NULL,
        [RefundedUtc] [datetime] NULL)
END

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[StripeRefundEvents]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[StripeRefundEvents](
        [StripePaymentIntentId] [nvarchar](255) NOT NULL CONSTRAINT [PK_StripeRefundEvents] PRIMARY KEY,
        [CreatedUtc] [datetime] NOT NULL)
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FilePurchases_UserId_FileId' AND object_id = OBJECT_ID(N'dbo.FilePurchases'))
    CREATE INDEX [IX_FilePurchases_UserId_FileId] ON [dbo].[FilePurchases]([UserId], [FileId])
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FilePurchases_BuyerEmail' AND object_id = OBJECT_ID(N'dbo.FilePurchases'))
    CREATE INDEX [IX_FilePurchases_BuyerEmail] ON [dbo].[FilePurchases]([BuyerEmail])
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FilePurchases_StripePaymentIntentId' AND object_id = OBJECT_ID(N'dbo.FilePurchases'))
    CREATE INDEX [IX_FilePurchases_StripePaymentIntentId] ON [dbo].[FilePurchases]([StripePaymentIntentId])
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FilePurchases_DownloadToken' AND object_id = OBJECT_ID(N'dbo.FilePurchases'))
    CREATE UNIQUE INDEX [IX_FilePurchases_DownloadToken] ON [dbo].[FilePurchases]([DownloadToken]) WHERE [DownloadToken] IS NOT NULL

IF NOT EXISTS (SELECT 1 FROM [dbo].[Settings] WHERE [SettingName] = N'StripeEnabled')
    INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue]) VALUES (N'StripeEnabled', N'False')
IF NOT EXISTS (SELECT 1 FROM [dbo].[Settings] WHERE [SettingName] = N'StripeSecretKey')
    INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue]) VALUES (N'StripeSecretKey', N'')
IF NOT EXISTS (SELECT 1 FROM [dbo].[Settings] WHERE [SettingName] = N'StripeWebhookSecret')
    INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue]) VALUES (N'StripeWebhookSecret', N'')
IF NOT EXISTS (SELECT 1 FROM [dbo].[Settings] WHERE [SettingName] = N'StripeCurrency')
    INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue]) VALUES (N'StripeCurrency', N'usd')
IF NOT EXISTS (SELECT 1 FROM [dbo].[Settings] WHERE [SettingName] = N'StripeAutomaticTax')
    INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue]) VALUES (N'StripeAutomaticTax', N'True')
IF NOT EXISTS (SELECT 1 FROM [dbo].[Settings] WHERE [SettingName] = N'StripeTaxBehavior')
    INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue]) VALUES (N'StripeTaxBehavior', N'exclusive')
IF NOT EXISTS (SELECT 1 FROM [dbo].[Settings] WHERE [SettingName] = N'StripeTaxCode')
    INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue]) VALUES (N'StripeTaxCode', N'')
IF NOT EXISTS (SELECT 1 FROM [dbo].[Settings] WHERE [SettingName] = N'StripeGuestLinkDays')
    INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue]) VALUES (N'StripeGuestLinkDays', N'7')
IF NOT EXISTS (SELECT 1 FROM [dbo].[Settings] WHERE [SettingName] = N'StripeGuestMaxDownloads')
    INSERT INTO [dbo].[Settings] ([SettingName], [SettingValue]) VALUES (N'StripeGuestMaxDownloads', N'5')

UPDATE [dbo].[Settings]
SET [SettingValue] = '10.04.00'
WHERE [SettingName] = 'VersionNumber'
