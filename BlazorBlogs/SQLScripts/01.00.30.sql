SET ANSI_NULLS ON
 
SET QUOTED_IDENTIFIER ON

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ExternalConnections]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[ExternalConnections](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ServerName] [nvarchar](1000) NOT NULL,
	[DatabaseName] [nvarchar](1000) NOT NULL,
	[IntegratedSecurity] [nvarchar](25) NOT NULL,
	[DatabaseUsername] [nvarchar](1000) NULL,
	[DatabasePassword] [nvarchar](1000) NULL,
 CONSTRAINT [PK_ExternalConnections] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END

UPDATE [dbo].[Settings]
   SET [SettingValue] = '01.00.30'
 WHERE [SettingName] = 'VersionNumber'