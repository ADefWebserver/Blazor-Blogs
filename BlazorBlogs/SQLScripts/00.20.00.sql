SET ANSI_NULLS ON
 
SET QUOTED_IDENTIFIER ON
 
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BlogCategory]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[BlogCategory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[BlogId] [int] NOT NULL,
	[CategoryId] [int] NOT NULL,
 CONSTRAINT [PK_BlogCategory] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END
 
SET ANSI_NULLS ON
 
SET QUOTED_IDENTIFIER ON
 
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Blogs]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Blogs](
	[BlogId] [int] IDENTITY(1,1) NOT NULL,
	[BlogDate] [datetime] NOT NULL,
	[BlogTitle] [nvarchar](500) NOT NULL,
	[BlogSummary] [nvarchar](max) NOT NULL,
	[BlogContent] [nvarchar](max) NOT NULL,
	[BlogUserName] [nvarchar](500) NOT NULL,
 CONSTRAINT [PK_Blogs] PRIMARY KEY CLUSTERED 
(
	[BlogId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
 
SET ANSI_NULLS ON
 
SET QUOTED_IDENTIFIER ON
 
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categorys]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Categorys](
	[CategoryId] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](500) NOT NULL,
	[Description] [nvarchar](500) NULL,
 CONSTRAINT [PK_Categorys] PRIMARY KEY CLUSTERED 
(
	[CategoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END
 
SET ANSI_NULLS ON
 
SET QUOTED_IDENTIFIER ON
 
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Comment]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Comment](
	[CommentId] [int] IDENTITY(1,1) NOT NULL,
	[BlogId] [int] NOT NULL,
	[ParentCommentId] [int] NULL,
	[CommentUserID] [nvarchar](500) NOT NULL,
	[Comment] [nvarchar](4000) NOT NULL,
	[CommentCreated] [datetime] NOT NULL,
	[CommentUpdated] [datetime] NULL,
	[CommentIPAddress] [nvarchar](500) NOT NULL,
 CONSTRAINT [PK_Comment] PRIMARY KEY CLUSTERED 
(
	[CommentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END
 
SET ANSI_NULLS ON
 
SET QUOTED_IDENTIFIER ON
 
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Logs]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Logs](
	[LogId] [int] IDENTITY(1,1) NOT NULL,
	[LogDate] [datetime] NOT NULL,
	[LogAction] [nvarchar](4000) NOT NULL,
	[LogUserName] [nvarchar](500) NULL,
	[LogIPAddress] [nvarchar](500) NOT NULL,
 CONSTRAINT [PK_Logs] PRIMARY KEY CLUSTERED 
(
	[LogId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END
 
SET ANSI_NULLS ON
 
SET QUOTED_IDENTIFIER ON
 

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_BlogCategory_Blogs]') AND parent_object_id = OBJECT_ID(N'[dbo].[BlogCategory]'))
ALTER TABLE [dbo].[BlogCategory]  WITH CHECK ADD  CONSTRAINT [FK_BlogCategory_Blogs] FOREIGN KEY([BlogId])
REFERENCES [dbo].[Blogs] ([BlogId])
ON DELETE CASCADE
 
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_BlogCategory_Blogs]') AND parent_object_id = OBJECT_ID(N'[dbo].[BlogCategory]'))
ALTER TABLE [dbo].[BlogCategory] CHECK CONSTRAINT [FK_BlogCategory_Blogs]
 
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_BlogCategory_Categorys]') AND parent_object_id = OBJECT_ID(N'[dbo].[BlogCategory]'))
ALTER TABLE [dbo].[BlogCategory]  WITH CHECK ADD  CONSTRAINT [FK_BlogCategory_Categorys] FOREIGN KEY([CategoryId])
REFERENCES [dbo].[Categorys] ([CategoryId])
ON DELETE CASCADE
 
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_BlogCategory_Categorys]') AND parent_object_id = OBJECT_ID(N'[dbo].[BlogCategory]'))
ALTER TABLE [dbo].[BlogCategory] CHECK CONSTRAINT [FK_BlogCategory_Categorys]
 
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Comment_Blogs]') AND parent_object_id = OBJECT_ID(N'[dbo].[Comment]'))
ALTER TABLE [dbo].[Comment]  WITH CHECK ADD  CONSTRAINT [FK_Comment_Blogs] FOREIGN KEY([BlogId])
REFERENCES [dbo].[Blogs] ([BlogId])
ON DELETE CASCADE
 
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Comment_Blogs]') AND parent_object_id = OBJECT_ID(N'[dbo].[Comment]'))
ALTER TABLE [dbo].[Comment] CHECK CONSTRAINT [FK_Comment_Blogs]
 
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Comment_Comment]') AND parent_object_id = OBJECT_ID(N'[dbo].[Comment]'))
ALTER TABLE [dbo].[Comment]  WITH CHECK ADD  CONSTRAINT [FK_Comment_Comment] FOREIGN KEY([ParentCommentId])
REFERENCES [dbo].[Comment] ([CommentId])
 
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Comment_Comment]') AND parent_object_id = OBJECT_ID(N'[dbo].[Comment]'))
ALTER TABLE [dbo].[Comment] CHECK CONSTRAINT [FK_Comment_Comment]
 

SET ANSI_NULLS ON
 
SET QUOTED_IDENTIFIER ON
 
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Files]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Files](
	[FileId] [int] IDENTITY(1,1) NOT NULL,
	[FileName] [nvarchar](500) NOT NULL,
	[FilePath] [nvarchar](4000) NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[DownloadCount] [int] NOT NULL,
 CONSTRAINT [PK_Files] PRIMARY KEY CLUSTERED 
(
	[FileId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END
 

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Settings]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Settings](
	[SettingID] [int] IDENTITY(1,1) NOT NULL,
	[SettingName] [nvarchar](150) NOT NULL,
	[SettingValue] [nvarchar](4000) NOT NULL,
 CONSTRAINT [PK_ADefHelpDesk_Settings] PRIMARY KEY CLUSTERED 
(
	[SettingID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

SET IDENTITY_INSERT [dbo].[Settings] ON 

INSERT [dbo].[Settings] ([SettingID], [SettingName], [SettingValue]) VALUES (1, N'AllowRegistration', N'True')
INSERT [dbo].[Settings] ([SettingID], [SettingName], [SettingValue]) VALUES (2, N'VerifiedRegistration', N'False')
INSERT [dbo].[Settings] ([SettingID], [SettingName], [SettingValue]) VALUES (3, N'ApplicationName', N'')
INSERT [dbo].[Settings] ([SettingID], [SettingName], [SettingValue]) VALUES (4, N'SMTPServer', N'')
INSERT [dbo].[Settings] ([SettingID], [SettingName], [SettingValue]) VALUES (5, N'SMTPSecure', N'False')
INSERT [dbo].[Settings] ([SettingID], [SettingName], [SettingValue]) VALUES (6, N'SMTPUserName', N'')
INSERT [dbo].[Settings] ([SettingID], [SettingName], [SettingValue]) VALUES (7, N'SMTPPassword', N'')
INSERT [dbo].[Settings] ([SettingID], [SettingName], [SettingValue]) VALUES (8, N'SMTPAuthendication', N'')
INSERT [dbo].[Settings] ([SettingID], [SettingName], [SettingValue]) VALUES (9, N'SMTPFromEmail', N'')
INSERT [dbo].[Settings] ([SettingID], [SettingName], [SettingValue]) VALUES (10, N'ApplicationLogo', N'uploads\logo.png')
INSERT [dbo].[Settings] ([SettingID], [SettingName], [SettingValue]) VALUES (11, N'ApplicationHeader', N'')
INSERT [dbo].[Settings] ([SettingID], [SettingName], [SettingValue]) VALUES (12, N'DisqusEnabled', N'False')
INSERT [dbo].[Settings] ([SettingID], [SettingName], [SettingValue]) VALUES (14, N'DisqusShortName', N'')
INSERT [dbo].[Settings] ([SettingID], [SettingName], [SettingValue]) VALUES (15, N'VersionNumber', N'00.20.00')

SET IDENTITY_INSERT [dbo].[Settings] OFF

END