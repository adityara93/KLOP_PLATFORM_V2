-- READ-ONLY V2-02 verification. Run only in the isolated V2 database.
SET NOCOUNT ON;
IF DB_NAME() <> N'KLOP_PLATFORM_V2_DB' THROW 51000, 'Wrong database', 1;
DECLARE @expected TABLE (S sysname,T sysname);
INSERT @expected VALUES ('identity','User'),('identity','UserCredential'),('identity','Role'),('identity','UserRole'),('identity','UserSession'),('identity','RefreshToken'),('tenant','Tenant'),('tenant','Page'),('tenant','TenantPageAccess'),('configuration','Application'),('configuration','ApplicationSetting'),('configuration','PlatformSetting'),('access','TenantApplicationAccess'),('monitoring','ApplicationHealth'),('monitoring','IntegrationStatus'),('audit','AuditLog');
IF EXISTS (SELECT 1 FROM @expected WHERE OBJECT_ID(QUOTENAME(S)+'.'+QUOTENAME(T),'U') IS NULL) THROW 51001, 'Missing table', 1;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory','U') IS NULL THROW 51002, 'Missing EF migration history', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20261021060000_V202InitialSchema') THROW 51003, 'Initial migration not recorded', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'identity.[User]') AND name=N'UX_User_SinglePlatform' AND is_unique=1 AND has_filter=1) THROW 51004, 'Platform uniqueness index missing', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'identity.[User]') AND name=N'UX_User_TenantUsername' AND is_unique=1 AND has_filter=1) THROW 51005, 'Tenant username index missing', 1;
IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'identity.UserRole') AND name IN (N'FK_UserRole_User_Scope',N'FK_UserRole_Role_Scope')) <> 2 THROW 51006, 'Role scope FKs missing', 1;
SELECT 'PASS' AS VerificationStatus, DB_NAME() AS DatabaseName, COUNT(*) AS ExpectedTables FROM @expected;
