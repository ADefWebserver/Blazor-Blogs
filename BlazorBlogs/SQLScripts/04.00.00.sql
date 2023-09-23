SET ANSI_NULLS ON

SET QUOTED_IDENTIFIER ON

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Newsletters]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Newsletters](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[NewsletterDate] [datetime] NOT NULL,
	[NewsletterTitle] [nvarchar](500) NOT NULL,
	[NewsletterContent] [nvarchar](max) NOT NULL,
	[Created] [datetime] NOT NULL,
	[Updated] [datetime] NULL,
 CONSTRAINT [PK_Newsletters] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END

SET ANSI_NULLS ON

SET QUOTED_IDENTIFIER ON

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NewslettersCampain]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[NewslettersCampain](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[NewsletterCampainName] [nvarchar](50) NOT NULL,
	[NewsletterId] [int] NOT NULL,
	[Created] [datetime] NOT NULL,
	[Updated] [datetime] NULL,
 CONSTRAINT [PK_NewslettersCampain] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END

SET ANSI_NULLS ON

SET QUOTED_IDENTIFIER ON

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NewslettersLogs]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[NewslettersLogs](
	[Id] [int] NOT NULL,
	[NewsletterCampainId] [int] NOT NULL,
	[LogType] [nvarchar](50) NULL,
	[UserName] [nvarchar](500) NULL,
	[LogDetails] [nvarchar](4000) NULL
) ON [PRIMARY]
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_NewslettersCampain_Newsletters]') AND parent_object_id = OBJECT_ID(N'[dbo].[NewslettersCampain]'))
ALTER TABLE [dbo].[NewslettersCampain]  WITH CHECK ADD  CONSTRAINT [FK_NewslettersCampain_Newsletters] FOREIGN KEY([NewsletterId])
REFERENCES [dbo].[Newsletters] ([Id])

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_NewslettersCampain_Newsletters]') AND parent_object_id = OBJECT_ID(N'[dbo].[NewslettersCampain]'))
ALTER TABLE [dbo].[NewslettersCampain] CHECK CONSTRAINT [FK_NewslettersCampain_Newsletters]

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_NewslettersLogs_NewslettersCampain]') AND parent_object_id = OBJECT_ID(N'[dbo].[NewslettersLogs]'))
ALTER TABLE [dbo].[NewslettersLogs]  WITH CHECK ADD  CONSTRAINT [FK_NewslettersLogs_NewslettersCampain] FOREIGN KEY([NewsletterCampainId])
REFERENCES [dbo].[NewslettersCampain] ([Id])

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_NewslettersLogs_NewslettersCampain]') AND parent_object_id = OBJECT_ID(N'[dbo].[NewslettersLogs]'))
ALTER TABLE [dbo].[NewslettersLogs] CHECK CONSTRAINT [FK_NewslettersLogs_NewslettersCampain]

SET ANSI_NULLS ON
 
SET QUOTED_IDENTIFIER ON

UPDATE [dbo].[Settings]
   SET [SettingValue] = '04.00.00'
 WHERE [SettingName] = 'VersionNumber'