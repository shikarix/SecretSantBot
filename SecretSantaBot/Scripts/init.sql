-- Create database if not exists (run manually or via migration)
USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SecretSanta')
BEGIN
    CREATE DATABASE SecretSanta;
END
GO

USE SecretSanta;
GO

-- Users table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [TelegramId] BIGINT NOT NULL UNIQUE,
        [Username] NVARCHAR(100) NOT NULL,
        [FirstName] NVARCHAR(100) NULL,
        [LastName] NVARCHAR(100) NULL,
        [RegisteredAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    CREATE INDEX IX_Users_TelegramId ON [dbo].[Users]([TelegramId]);
END
GO

-- Rooms table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Rooms]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Rooms] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        [CreatorTelegramId] BIGINT NOT NULL,
        [CreatorUsername] NVARCHAR(100) NOT NULL,
        [CreatorFirstName] NVARCHAR(100) NULL,
        [Code] NVARCHAR(20) NULL UNIQUE,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [DrawDate] DATETIME2 NULL,
        [IsDrawn] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX IX_Rooms_Code ON [dbo].[Rooms]([Code]);
    CREATE INDEX IX_Rooms_CreatorTelegramId ON [dbo].[Rooms]([CreatorTelegramId]);
END
GO

-- RoomParticipants table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoomParticipants]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RoomParticipants] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [RoomId] INT NOT NULL,
        [TelegramId] BIGINT NOT NULL,
        [Username] NVARCHAR(100) NOT NULL,
        [FirstName] NVARCHAR(100) NULL,
        [WishList] NVARCHAR(MAX) NULL,
        [JoinedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY ([RoomId]) REFERENCES [dbo].[Rooms]([Id]) ON DELETE CASCADE,
        UNIQUE([RoomId], [TelegramId])
    );
    CREATE INDEX IX_RoomParticipants_RoomId ON [dbo].[RoomParticipants]([RoomId]);
    CREATE INDEX IX_RoomParticipants_TelegramId ON [dbo].[RoomParticipants]([TelegramId]);
END
GO

-- Assignments table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Assignments] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [RoomId] INT NOT NULL,
        [SantaTelegramId] BIGINT NOT NULL,
        [RecipientTelegramId] BIGINT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY ([RoomId]) REFERENCES [dbo].[Rooms]([Id]) ON DELETE CASCADE,
        UNIQUE([RoomId], [SantaTelegramId])
    );
    CREATE INDEX IX_Assignments_RoomId ON [dbo].[Assignments]([RoomId]);
    CREATE INDEX IX_Assignments_SantaTelegramId ON [dbo].[Assignments]([SantaTelegramId]);
END
GO

-- Invitations table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Invitations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Invitations] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [RoomId] INT NOT NULL,
        [InvitedTelegramId] BIGINT NOT NULL,
        [Code] NVARCHAR(20) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsAccepted] BIT NOT NULL DEFAULT 0,
        FOREIGN KEY ([RoomId]) REFERENCES [dbo].[Rooms]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX IX_Invitations_RoomId ON [dbo].[Invitations]([RoomId]);
    CREATE INDEX IX_Invitations_Code ON [dbo].[Invitations]([Code]);
END
GO

