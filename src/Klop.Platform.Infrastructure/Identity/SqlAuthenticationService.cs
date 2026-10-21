using System.Data;
using System.Security.Cryptography;
using Klop.Platform.Application.Identity;
using Klop.Platform.Domain.Identity;
using Microsoft.Data.SqlClient;

namespace Klop.Platform.Infrastructure.Identity;

/// <summary>Database-serialized login and session management; raw tokens are never persisted.</summary>
public sealed class SqlAuthenticationService(string connectionString, IPasswordHasher hasher,
    IRefreshTokenGenerator tokenGenerator, IAccessTokenIssuer issuer, TimeProvider clock)
{
    private const int MaxFailures = 5;
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(7);

    public async Task<AuthenticationResult?> LoginAsync(string? loginText, string? password, string? ip, string? userAgent, CancellationToken ct)
    {
        if (!LoginIdentity.TryParse(loginText, out var login) || login is null || string.IsNullOrEmpty(password)) return null;
        await using var db = new SqlConnection(connectionString);
        await db.OpenAsync(ct);
        await using var tx = (SqlTransaction)await db.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            await using var find = new SqlCommand(@"SELECT TOP (1) u.UserId,u.TenantId,u.Scope,u.IsActive,
CAST(COALESCE(t.IsActive,1) AS bit),c.PasswordHash,c.FailedLoginCount,c.LockoutEndAt
FROM [identity].[User] u WITH (UPDLOCK,HOLDLOCK)
JOIN [identity].[UserCredential] c WITH (UPDLOCK,HOLDLOCK) ON c.UserId=u.UserId
LEFT JOIN [tenant].[Tenant] t WITH (HOLDLOCK) ON t.TenantId=u.TenantId
WHERE u.NormalizedUsername=@name AND
((@code='PLATFORM' AND u.Scope='PLATFORM' AND u.TenantId IS NULL AND UPPER(u.Username)='ADMIN')
OR (@code<>'PLATFORM' AND u.Scope='TENANT' AND t.TenantCode=@code));", db, tx);
            find.Parameters.Add("@name",SqlDbType.NVarChar,100).Value=login.NormalizedUsername;
            find.Parameters.Add("@code",SqlDbType.VarChar,30).Value=login.TenantCode;
            long uid; int? tenant; string scope; bool active; string hash; int failures; DateTime? locked;
            await using (var rd=await find.ExecuteReaderAsync(CommandBehavior.SingleRow,ct))
            {
                if (!await rd.ReadAsync(ct)) { await rd.DisposeAsync(); await tx.CommitAsync(ct); return null; }
                uid=rd.GetInt64(0); tenant=rd.IsDBNull(1)?null:rd.GetInt32(1);
                scope=rd.GetString(2); active=rd.GetBoolean(3)&&rd.GetBoolean(4);
                hash=rd.GetString(5); failures=rd.GetInt32(6);
                locked=rd.IsDBNull(7)?null:DateTime.SpecifyKind(rd.GetDateTime(7),DateTimeKind.Utc);
            }
            var now=clock.GetUtcNow();
            if (!active || scope!=(login.IsPlatform?"PLATFORM":"TENANT") ||
                (login.IsPlatform?tenant is not null:tenant is null or <=0) ||
                (locked is { } until && until>now.UtcDateTime))
            {
                await tx.CommitAsync(ct); return null;
            }
            var verified=hasher.Verify(hash,password);
            if (verified==PasswordVerificationResult.Failed)
            {
                await using var fail=new SqlCommand(@"UPDATE [identity].[UserCredential] SET
FailedLoginCount=CASE WHEN LockoutEndAt IS NOT NULL AND LockoutEndAt<=@now THEN 1
WHEN FailedLoginCount>=@max THEN @max ELSE FailedLoginCount+1 END,
LockoutEndAt=CASE WHEN LockoutEndAt>@now THEN LockoutEndAt
WHEN (CASE WHEN LockoutEndAt IS NOT NULL AND LockoutEndAt<=@now THEN 1 ELSE FailedLoginCount+1 END)>=@max THEN @until ELSE NULL END
WHERE UserId=@uid;
INSERT INTO [audit].[AuditLog](ActorUserId,ActorTenantId,TargetTenantId,ResourceType,ResourceId,Action,Outcome,OccurredAt)
VALUES(@uid,@tenant,@tenant,'IDENTITY',CONVERT(nvarchar(150),@uid),'LOGIN','FAILED',@now);",db,tx);
                fail.Parameters.Add("@uid",SqlDbType.BigInt).Value=uid;
                fail.Parameters.Add("@tenant",SqlDbType.Int).Value=(object?)tenant??DBNull.Value;
                fail.Parameters.Add("@max",SqlDbType.Int).Value=MaxFailures;
                fail.Parameters.Add("@now",SqlDbType.DateTime2).Value=now.UtcDateTime;
                fail.Parameters.Add("@until",SqlDbType.DateTime2).Value=now.AddMinutes(15).UtcDateTime;
                await fail.ExecuteNonQueryAsync(ct);
                await tx.CommitAsync(ct); return null;
            }
            if (failures>=MaxFailures && locked is null) { await tx.CommitAsync(ct); return null; }
            var session=Guid.NewGuid(); var family=Guid.NewGuid(); var expiry=now.Add(SessionLifetime);
            var refresh=tokenGenerator.Generate();
            // Token signing happens before transaction commit: failure cannot leave a live session.
            var access=issuer.Issue(new AccessTokenPrincipal(uid,tenant,scope,session));
            await using var success=new SqlCommand(@"INSERT INTO [identity].[UserSession](SessionId,UserId,ExpiresAt,IpAddress,UserAgent)
VALUES(@sid,@uid,@expires,@ip,@ua);
INSERT INTO [identity].[RefreshToken](SessionId,TokenHash,FamilyId,ExpiresAt)
VALUES(@sid,@hash,@family,@expires);
UPDATE [identity].[UserCredential] SET FailedLoginCount=0,LockoutEndAt=NULL WHERE UserId=@uid;
INSERT INTO [audit].[AuditLog](ActorUserId,ActorTenantId,TargetTenantId,ResourceType,ResourceId,Action,Outcome,OccurredAt)
VALUES(@uid,@tenant,@tenant,'IDENTITY',CONVERT(nvarchar(150),@uid),'LOGIN','SUCCESS',@now);",db,tx);
            success.Parameters.Add("@sid",SqlDbType.UniqueIdentifier).Value=session;
            success.Parameters.Add("@uid",SqlDbType.BigInt).Value=uid;
            success.Parameters.Add("@expires",SqlDbType.DateTime2).Value=expiry.UtcDateTime;
            success.Parameters.Add("@ip",SqlDbType.VarChar,45).Value=(object?)ip??DBNull.Value;
            success.Parameters.Add("@ua",SqlDbType.NVarChar,512).Value=(object?)userAgent??DBNull.Value;
            success.Parameters.Add("@hash",SqlDbType.VarBinary,32).Value=refresh.TokenHash;
            success.Parameters.Add("@family",SqlDbType.UniqueIdentifier).Value=family;
            success.Parameters.Add("@tenant",SqlDbType.Int).Value=(object?)tenant??DBNull.Value;
            success.Parameters.Add("@now",SqlDbType.DateTime2).Value=now.UtcDateTime;
            await success.ExecuteNonQueryAsync(ct);
            await tx.CommitAsync(ct);
            return new AuthenticationResult(access.Token,access.ExpiresAt,refresh.Token,expiry,session);
        }
        catch { await tx.RollbackAsync(CancellationToken.None); throw; }
    }

    public async Task<AuthenticationResult?> RefreshAsync(string? presented, IUserSessionStore store, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(presented) || presented.Length!=43 ||
            presented.Any(c=>!(char.IsAsciiLetterOrDigit(c)||c is '-' or '_'))) return null;
        var now=clock.GetUtcNow();
        var replacement=tokenGenerator.Generate();
        var hash=tokenGenerator.Hash(presented);
        // Rotation and identity validation are serialized in the store, followed by a live account check.
        var result=await store.RotateAsync(hash,replacement.TokenHash,now.Add(SessionLifetime),now,ct);
        if (result.Status!=RefreshRotationStatus.Success || result.SessionId is null || result.UserId is null) return null;
        await using var db=new SqlConnection(connectionString);
        await db.OpenAsync(ct);
        await using var cmd=new SqlCommand(@"SELECT u.TenantId,u.Scope FROM [identity].[User] u
LEFT JOIN [tenant].[Tenant] t ON t.TenantId=u.TenantId
JOIN [identity].[UserSession] s ON s.UserId=u.UserId
WHERE u.UserId=@uid AND s.SessionId=@sid AND u.IsActive=1 AND s.RevokedAt IS NULL
AND s.ExpiresAt>@now AND (u.Scope='PLATFORM' AND u.TenantId IS NULL
OR u.Scope='TENANT' AND u.TenantId IS NOT NULL AND t.IsActive=1);",db);
        cmd.Parameters.Add("@uid",SqlDbType.BigInt).Value=result.UserId.Value;
        cmd.Parameters.Add("@sid",SqlDbType.UniqueIdentifier).Value=result.SessionId.Value;
        cmd.Parameters.Add("@now",SqlDbType.DateTime2).Value=now.UtcDateTime;
        await using var rd=await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow,ct);
        if (!await rd.ReadAsync(ct)) { await rd.DisposeAsync(); await store.RevokeAsync(result.SessionId.Value,now,ct); return null; }
        int? tenant=rd.IsDBNull(0)?null:rd.GetInt32(0);
        var scope=rd.GetString(1);
        await rd.DisposeAsync();
        var access=issuer.Issue(new AccessTokenPrincipal(result.UserId.Value,tenant,scope,result.SessionId.Value));
        return new AuthenticationResult(access.Token,access.ExpiresAt,replacement.Token,now.Add(SessionLifetime),result.SessionId.Value);
    }

    public async Task<bool> IsSessionActiveAsync(long userId,Guid sessionId,string scope,int? tenant,CancellationToken ct)
    {
        await using var db=new SqlConnection(connectionString); await db.OpenAsync(ct);
        await using var cmd=new SqlCommand(@"SELECT COUNT_BIG(1) FROM [identity].[UserSession] s
JOIN [identity].[User] u ON u.UserId=s.UserId
LEFT JOIN [tenant].[Tenant] t ON t.TenantId=u.TenantId
WHERE s.SessionId=@sid AND u.UserId=@uid AND u.Scope=@scope
AND ((@scope='PLATFORM' AND u.TenantId IS NULL AND @tenant IS NULL)
OR (@scope='TENANT' AND u.TenantId=@tenant AND t.IsActive=1))
AND u.IsActive=1 AND s.RevokedAt IS NULL AND s.ExpiresAt>@now;",db);
        cmd.Parameters.Add("@sid",SqlDbType.UniqueIdentifier).Value=sessionId;
        cmd.Parameters.Add("@uid",SqlDbType.BigInt).Value=userId;
        cmd.Parameters.Add("@scope",SqlDbType.VarChar,10).Value=scope;
        cmd.Parameters.Add("@tenant",SqlDbType.Int).Value=(object?)tenant??DBNull.Value;
        cmd.Parameters.Add("@now",SqlDbType.DateTime2).Value=clock.GetUtcNow().UtcDateTime;
        return (long)(await cmd.ExecuteScalarAsync(ct)??0L)==1;
    }
}
