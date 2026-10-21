-- KLOP Platform V2 | V2-02 | SQL Server 2022+ | Schema baseline
-- Run against a NEW, empty KLOP_PLATFORM_V2_DB. Review before execution.
-- Never run this script against KLOP_PLATFORM V1. No seed credentials.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF SCHEMA_ID('tenant') IS NULL EXEC('CREATE SCHEMA [tenant]');
IF SCHEMA_ID('identity') IS NULL EXEC('CREATE SCHEMA [identity]');
IF SCHEMA_ID('configuration') IS NULL EXEC('CREATE SCHEMA [configuration]');
IF SCHEMA_ID('access') IS NULL EXEC('CREATE SCHEMA [access]');
IF SCHEMA_ID('monitoring') IS NULL EXEC('CREATE SCHEMA [monitoring]');
IF SCHEMA_ID('audit') IS NULL EXEC('CREATE SCHEMA [audit]');

CREATE TABLE [tenant].[Tenant] (
    [TenantId] int IDENTITY(1,1) NOT NULL,
    [TenantCode] varchar(30) NOT NULL,
    [TenantName] nvarchar(200) NOT NULL,
    [IsActive] bit NOT NULL CONSTRAINT DF_Tenant_IsActive DEFAULT(1),
    [CreatedAt] datetime2(3) NOT NULL CONSTRAINT DF_Tenant_CreatedAt DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] datetime2(3) NOT NULL CONSTRAINT DF_Tenant_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Tenant] PRIMARY KEY ([TenantId])
);

CREATE TABLE [identity].[User] (
    [UserId] bigint IDENTITY(1,1) NOT NULL,
    [TenantId] int NULL,
    [Scope] varchar(10) NOT NULL,
    [Username] nvarchar(100) NOT NULL,
    [NormalizedUsername] nvarchar(100) NOT NULL,
    [DisplayName] nvarchar(200) NOT NULL,
    [Email] nvarchar(254) NULL,
    [IsActive] bit NOT NULL DEFAULT(1),
    [CreatedAt] datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_User] PRIMARY KEY ([UserId])
);

CREATE TABLE [identity].[UserCredential] (
    [UserId] bigint NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [FailedLoginCount] int NOT NULL DEFAULT(0),
    [LockoutEndAt] datetime2(3) NULL,
    [PasswordChangedAt] datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_UserCredential] PRIMARY KEY ([UserId])
);

CREATE TABLE [identity].[Role] (
    [RoleId] int IDENTITY(1,1) NOT NULL,
    [RoleCode] varchar(50) NOT NULL,
    [RoleName] nvarchar(100) NOT NULL,
    [Scope] varchar(10) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT(1),
    CONSTRAINT [PK_Role] PRIMARY KEY ([RoleId])
);

CREATE TABLE [identity].[UserRole] (
    [UserId] bigint NOT NULL,
    [RoleId] int NOT NULL,
    [Scope] varchar(10) NOT NULL,
    [AssignedAt] datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_UserRole] PRIMARY KEY ([UserId], [RoleId])
);

CREATE TABLE [identity].[UserSession] (
    [SessionId] uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
    [UserId] bigint NOT NULL,
    [CreatedAt] datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    [ExpiresAt] datetime2(3) NOT NULL,
    [RevokedAt] datetime2(3) NULL,
    [IpAddress] varchar(45) NULL,
    [UserAgent] nvarchar(512) NULL,
    CONSTRAINT [PK_UserSession] PRIMARY KEY ([SessionId])
);

CREATE TABLE [identity].[RefreshToken] (
    [RefreshTokenId] bigint IDENTITY(1,1) NOT NULL,
    [SessionId] uniqueidentifier NOT NULL,
    [TokenHash] varbinary(32) NOT NULL,
    [FamilyId] uniqueidentifier NOT NULL,
    [ReplacedByTokenId] bigint NULL,
    [CreatedAt] datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    [ExpiresAt] datetime2(3) NOT NULL,
    [UsedAt] datetime2(3) NULL,
    [RevokedAt] datetime2(3) NULL,
    CONSTRAINT [PK_RefreshToken] PRIMARY KEY ([RefreshTokenId])
);

CREATE TABLE [tenant].[Page] (
    [PageId] int IDENTITY(1,1) NOT NULL,
    [PageCode] varchar(100) NOT NULL,
    [PageName] nvarchar(150) NOT NULL,
    [Route] nvarchar(300) NOT NULL,
    [ParentPageId] int NULL,
    [SortOrder] int NOT NULL DEFAULT(0),
    [IsActive] bit NOT NULL DEFAULT(1),
    CONSTRAINT [PK_Page] PRIMARY KEY ([PageId])
);

CREATE TABLE [tenant].[TenantPageAccess] (
    [TenantId] int NOT NULL,
    [PageId] int NOT NULL,
    [IsEnabled] bit NOT NULL DEFAULT(1),
    CONSTRAINT [PK_TenantPageAccess] PRIMARY KEY ([TenantId], [PageId])
);

CREATE TABLE [configuration].[Application] (
    [ApplicationId] int IDENTITY(1,1) NOT NULL,
    [ApplicationCode] varchar(80) NOT NULL,
    [ApplicationName] nvarchar(200) NOT NULL,
    [LaunchUrl] nvarchar(2048) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT(1),
    CONSTRAINT [PK_Application] PRIMARY KEY ([ApplicationId])
);

CREATE TABLE [configuration].[ApplicationSetting] (
    [ApplicationSettingId] bigint IDENTITY(1,1) NOT NULL,
    [ApplicationId] int NOT NULL,
    [SettingKey] varchar(150) NOT NULL,
    [SettingValue] nvarchar(max) NULL,
    [SecretRef] nvarchar(500) NULL,
    CONSTRAINT [PK_ApplicationSetting] PRIMARY KEY ([ApplicationSettingId])
);

CREATE TABLE [configuration].[PlatformSetting] (
    [PlatformSettingId] bigint IDENTITY(1,1) NOT NULL,
    [SettingKey] varchar(150) NOT NULL,
    [SettingValue] nvarchar(max) NULL,
    [SecretRef] nvarchar(500) NULL,
    CONSTRAINT [PK_PlatformSetting] PRIMARY KEY ([PlatformSettingId])
);

CREATE TABLE [access].[TenantApplicationAccess] (
    [TenantId] int NOT NULL,
    [ApplicationId] int NOT NULL,
    [IsEnabled] bit NOT NULL DEFAULT(1),
    CONSTRAINT [PK_TenantApplicationAccess] PRIMARY KEY ([TenantId], [ApplicationId])
);

CREATE TABLE [monitoring].[ApplicationHealth] (
    [ApplicationHealthId] bigint IDENTITY(1,1) NOT NULL,
    [ApplicationId] int NOT NULL,
    [TenantId] int NULL,
    [Status] varchar(20) NOT NULL,
    [ObservedAt] datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    [LatencyMs] int NULL,
    [Details] nvarchar(1000) NULL,
    CONSTRAINT [PK_ApplicationHealth] PRIMARY KEY ([ApplicationHealthId])
);

CREATE TABLE [monitoring].[IntegrationStatus] (
    [IntegrationStatusId] bigint IDENTITY(1,1) NOT NULL,
    [ApplicationId] int NOT NULL,
    [TenantId] int NULL,
    [IntegrationCode] varchar(100) NOT NULL,
    [Status] varchar(20) NOT NULL,
    [ObservedAt] datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    [Details] nvarchar(1000) NULL,
    CONSTRAINT [PK_IntegrationStatus] PRIMARY KEY ([IntegrationStatusId])
);

CREATE TABLE [audit].[AuditLog] (
    [AuditLogId] bigint IDENTITY(1,1) NOT NULL,
    [ActorUserId] bigint NULL,
    [ActorTenantId] int NULL,
    [TargetTenantId] int NULL,
    [ResourceType] varchar(100) NOT NULL,
    [ResourceId] nvarchar(150) NULL,
    [Action] varchar(100) NOT NULL,
    [Outcome] varchar(20) NOT NULL,
    [OccurredAt] datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    [CorrelationId] uniqueidentifier NULL,
    [RequestMetadata] nvarchar(2000) NULL,
    CONSTRAINT [PK_AuditLog] PRIMARY KEY ([AuditLogId])
);
ALTER TABLE [identity].[User] ADD CONSTRAINT [FK_User_TenantId_Tenant] FOREIGN KEY ([TenantId]) REFERENCES [tenant].[Tenant]([TenantId]);
ALTER TABLE [identity].[UserCredential] ADD CONSTRAINT [FK_UserCredential_UserId_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([UserId]);
ALTER TABLE [identity].[UserRole] ADD CONSTRAINT [FK_UserRole_UserId_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([UserId]);
ALTER TABLE [identity].[UserRole] ADD CONSTRAINT [FK_UserRole_RoleId_Role] FOREIGN KEY ([RoleId]) REFERENCES [identity].[Role]([RoleId]);
ALTER TABLE [identity].[UserSession] ADD CONSTRAINT [FK_UserSession_UserId_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([UserId]);
ALTER TABLE [identity].[RefreshToken] ADD CONSTRAINT [FK_RefreshToken_SessionId_UserSession] FOREIGN KEY ([SessionId]) REFERENCES [identity].[UserSession]([SessionId]);
ALTER TABLE [identity].[RefreshToken] ADD CONSTRAINT [FK_RefreshToken_ReplacedByTokenId_RefreshToken] FOREIGN KEY ([ReplacedByTokenId]) REFERENCES [identity].[RefreshToken]([RefreshTokenId]);
ALTER TABLE [tenant].[Page] ADD CONSTRAINT [FK_Page_ParentPageId_Page] FOREIGN KEY ([ParentPageId]) REFERENCES [tenant].[Page]([PageId]);
ALTER TABLE [tenant].[TenantPageAccess] ADD CONSTRAINT [FK_TenantPageAccess_TenantId_Tenant] FOREIGN KEY ([TenantId]) REFERENCES [tenant].[Tenant]([TenantId]);
ALTER TABLE [tenant].[TenantPageAccess] ADD CONSTRAINT [FK_TenantPageAccess_PageId_Page] FOREIGN KEY ([PageId]) REFERENCES [tenant].[Page]([PageId]);
ALTER TABLE [configuration].[ApplicationSetting] ADD CONSTRAINT [FK_ApplicationSetting_ApplicationId_Application] FOREIGN KEY ([ApplicationId]) REFERENCES [configuration].[Application]([ApplicationId]);
ALTER TABLE [access].[TenantApplicationAccess] ADD CONSTRAINT [FK_TenantApplicationAccess_TenantId_Tenant] FOREIGN KEY ([TenantId]) REFERENCES [tenant].[Tenant]([TenantId]);
ALTER TABLE [access].[TenantApplicationAccess] ADD CONSTRAINT [FK_TenantApplicationAccess_ApplicationId_Application] FOREIGN KEY ([ApplicationId]) REFERENCES [configuration].[Application]([ApplicationId]);
ALTER TABLE [monitoring].[ApplicationHealth] ADD CONSTRAINT [FK_ApplicationHealth_ApplicationId_Application] FOREIGN KEY ([ApplicationId]) REFERENCES [configuration].[Application]([ApplicationId]);
ALTER TABLE [monitoring].[ApplicationHealth] ADD CONSTRAINT [FK_ApplicationHealth_TenantId_Tenant] FOREIGN KEY ([TenantId]) REFERENCES [tenant].[Tenant]([TenantId]);
ALTER TABLE [monitoring].[IntegrationStatus] ADD CONSTRAINT [FK_IntegrationStatus_ApplicationId_Application] FOREIGN KEY ([ApplicationId]) REFERENCES [configuration].[Application]([ApplicationId]);
ALTER TABLE [monitoring].[IntegrationStatus] ADD CONSTRAINT [FK_IntegrationStatus_TenantId_Tenant] FOREIGN KEY ([TenantId]) REFERENCES [tenant].[Tenant]([TenantId]);
ALTER TABLE [audit].[AuditLog] ADD CONSTRAINT [FK_AuditLog_ActorUserId_User] FOREIGN KEY ([ActorUserId]) REFERENCES [identity].[User]([UserId]);
ALTER TABLE [audit].[AuditLog] ADD CONSTRAINT [FK_AuditLog_ActorTenantId_Tenant] FOREIGN KEY ([ActorTenantId]) REFERENCES [tenant].[Tenant]([TenantId]);
ALTER TABLE [audit].[AuditLog] ADD CONSTRAINT [FK_AuditLog_TargetTenantId_Tenant] FOREIGN KEY ([TargetTenantId]) REFERENCES [tenant].[Tenant]([TenantId]);
CREATE UNIQUE INDEX [UX_Tenant_TenantCode] ON [tenant].[Tenant] ([TenantCode]);
CREATE UNIQUE INDEX [UX_Role_RoleCode] ON [identity].[Role] ([RoleCode]);
CREATE UNIQUE INDEX [UX_User_UserId_Scope] ON [identity].[User] ([UserId], [Scope]);
CREATE UNIQUE INDEX [UX_Role_RoleId_Scope] ON [identity].[Role] ([RoleId], [Scope]);
CREATE UNIQUE INDEX [UX_RefreshToken_TokenHash] ON [identity].[RefreshToken] ([TokenHash]);
CREATE UNIQUE INDEX [UX_Page_PageCode] ON [tenant].[Page] ([PageCode]);
CREATE UNIQUE INDEX [UX_Page_Route] ON [tenant].[Page] ([Route]);
CREATE UNIQUE INDEX [UX_Application_ApplicationCode] ON [configuration].[Application] ([ApplicationCode]);
CREATE UNIQUE INDEX [UX_ApplicationSetting_ApplicationId_SettingKey] ON [configuration].[ApplicationSetting] ([ApplicationId], [SettingKey]);
CREATE UNIQUE INDEX [UX_PlatformSetting_SettingKey] ON [configuration].[PlatformSetting] ([SettingKey]);
CREATE UNIQUE INDEX [UX_User_SinglePlatform] ON [identity].[User]([Scope]) WHERE [Scope]='PLATFORM';
CREATE UNIQUE INDEX [UX_User_TenantUsername] ON [identity].[User]([TenantId],[NormalizedUsername]) WHERE [Scope]='TENANT';
ALTER TABLE [tenant].[Tenant] ADD CONSTRAINT [CK_Tenant_1] CHECK (TenantCode <> 'PLATFORM' AND TenantCode NOT LIKE '%@%' AND LEN(LTRIM(RTRIM(TenantCode))) > 0);
ALTER TABLE [identity].[User] ADD CONSTRAINT [CK_User_1] CHECK ((Scope = 'PLATFORM' AND TenantId IS NULL AND Username = 'admin' AND NormalizedUsername = 'ADMIN') OR (Scope = 'TENANT' AND TenantId IS NOT NULL AND Username NOT LIKE '%@%' AND LEN(LTRIM(RTRIM(Username))) > 0));
ALTER TABLE [identity].[Role] ADD CONSTRAINT [CK_Role_1] CHECK (Scope IN ('PLATFORM','TENANT'));
ALTER TABLE [identity].[UserRole] ADD CONSTRAINT [CK_UserRole_1] CHECK (Scope IN ('PLATFORM','TENANT'));
ALTER TABLE [identity].[UserCredential] ADD CONSTRAINT [CK_UserCredential_1] CHECK (FailedLoginCount >= 0);
ALTER TABLE [configuration].[ApplicationSetting] ADD CONSTRAINT [CK_ApplicationSetting_1] CHECK ((SettingValue IS NULL OR SecretRef IS NULL));
ALTER TABLE [configuration].[PlatformSetting] ADD CONSTRAINT [CK_PlatformSetting_1] CHECK ((SettingValue IS NULL OR SecretRef IS NULL));
ALTER TABLE [monitoring].[ApplicationHealth] ADD CONSTRAINT [CK_ApplicationHealth_1] CHECK (LatencyMs IS NULL OR LatencyMs >= 0);
-- Enforce role scope at the database boundary using composite foreign keys.
ALTER TABLE [identity].[UserRole] ADD CONSTRAINT [FK_UserRole_User_Scope] FOREIGN KEY ([UserId],[Scope]) REFERENCES [identity].[User]([UserId],[Scope]);
ALTER TABLE [identity].[UserRole] ADD CONSTRAINT [FK_UserRole_Role_Scope] FOREIGN KEY ([RoleId],[Scope]) REFERENCES [identity].[Role]([RoleId],[Scope]);
-- TenantCode is case-insensitive under database collation; use explicit CI collation for consistent identity semantics.
-- Additional indexes for hot access paths.
CREATE INDEX [IX_UserSession_UserId] ON [identity].[UserSession]([UserId]);
CREATE INDEX [IX_RefreshToken_SessionId] ON [identity].[RefreshToken]([SessionId]);
CREATE INDEX [IX_AuditLog_OccurredAt] ON [audit].[AuditLog]([OccurredAt] DESC);
CREATE INDEX [IX_AuditLog_ActorUserId] ON [audit].[AuditLog]([ActorUserId],[OccurredAt] DESC);
COMMIT TRANSACTION;
