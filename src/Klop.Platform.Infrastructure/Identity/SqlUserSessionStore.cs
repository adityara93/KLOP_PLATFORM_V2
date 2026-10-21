using System.Data;
using Klop.Platform.Application.Identity;
using Microsoft.Data.SqlClient;

namespace Klop.Platform.Infrastructure.Identity;

/// <summary>Atomic SQL Server session and refresh-token operations.</summary>
public sealed class SqlUserSessionStore(string connectionString) : IUserSessionStore
{
    public async Task CreateAsync(NewUserSession session, CancellationToken cancellationToken = default)
    {
        if (session.SessionId == Guid.Empty || session.TokenFamilyId == Guid.Empty || session.UserId <= 0 ||
            session.RefreshTokenHash.Length != 32 || session.RefreshTokenExpiresAt > session.SessionExpiresAt)
            throw new ArgumentException("Invalid session.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            await using var command = new SqlCommand(@"INSERT INTO [identity].[UserSession]
(SessionId,UserId,ExpiresAt,IpAddress,UserAgent) VALUES (@sid,@uid,@expires,@ip,@ua);
INSERT INTO [identity].[RefreshToken]
(SessionId,TokenHash,FamilyId,ExpiresAt) VALUES (@sid,@hash,@family,@refreshExpires);", connection, tx);
            command.Parameters.Add("@sid", SqlDbType.UniqueIdentifier).Value = session.SessionId;
            command.Parameters.Add("@uid", SqlDbType.BigInt).Value = session.UserId;
            command.Parameters.Add("@expires", SqlDbType.DateTime2).Value = session.SessionExpiresAt.UtcDateTime;
            command.Parameters.Add("@ip", SqlDbType.VarChar, 45).Value = (object?)session.IpAddress ?? DBNull.Value;
            command.Parameters.Add("@ua", SqlDbType.NVarChar, 512).Value = (object?)session.UserAgent ?? DBNull.Value;
            command.Parameters.Add("@hash", SqlDbType.VarBinary, 32).Value = session.RefreshTokenHash;
            command.Parameters.Add("@family", SqlDbType.UniqueIdentifier).Value = session.TokenFamilyId;
            command.Parameters.Add("@refreshExpires", SqlDbType.DateTime2).Value = session.RefreshTokenExpiresAt.UtcDateTime;
            await command.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch { await tx.RollbackAsync(CancellationToken.None); throw; }
    }

    public async Task<RefreshRotationResult> RotateAsync(byte[] presentedTokenHash, byte[] replacementTokenHash, DateTimeOffset replacementExpiry, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (presentedTokenHash.Length != 32 || replacementTokenHash.Length != 32 ||
            System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(presentedTokenHash, replacementTokenHash))
            return new RefreshRotationResult(RefreshRotationStatus.Invalid, null, null);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            await using var lookup = new SqlCommand(@"SELECT TOP (1) r.RefreshTokenId,r.SessionId,s.UserId,r.FamilyId,r.UsedAt,r.RevokedAt,r.ExpiresAt,s.RevokedAt,s.ExpiresAt
FROM [identity].[RefreshToken] r WITH (UPDLOCK,HOLDLOCK)
JOIN [identity].[UserSession] s WITH (UPDLOCK,HOLDLOCK) ON s.SessionId=r.SessionId
WHERE r.TokenHash=@hash", connection, tx);
            lookup.Parameters.Add("@hash", SqlDbType.VarBinary, 32).Value = presentedTokenHash;
            await using var reader = await lookup.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                await reader.DisposeAsync();
                await tx.CommitAsync(cancellationToken);
                return new RefreshRotationResult(RefreshRotationStatus.Invalid, null, null);
            }
            var tokenId = reader.GetInt64(0);
            var sessionId = reader.GetGuid(1);
            var userId = reader.GetInt64(2);
            var familyId = reader.GetGuid(3);
            var used = !reader.IsDBNull(4);
            var tokenRevoked = !reader.IsDBNull(5);
            var tokenExpiry = DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc);
            var sessionRevoked = !reader.IsDBNull(7);
            var sessionExpiry = DateTime.SpecifyKind(reader.GetDateTime(8), DateTimeKind.Utc);
            await reader.DisposeAsync();
            if (used)
            {
                await using var revoke = new SqlCommand(@"UPDATE [identity].[UserSession] SET RevokedAt=COALESCE(RevokedAt,@now) WHERE SessionId=@sid;
UPDATE [identity].[RefreshToken] SET RevokedAt=COALESCE(RevokedAt,@now) WHERE FamilyId=@family;", connection, tx);
                revoke.Parameters.Add("@now", SqlDbType.DateTime2).Value = now.UtcDateTime;
                revoke.Parameters.Add("@sid", SqlDbType.UniqueIdentifier).Value = sessionId;
                revoke.Parameters.Add("@family", SqlDbType.UniqueIdentifier).Value = familyId;
                await revoke.ExecuteNonQueryAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return new RefreshRotationResult(RefreshRotationStatus.Replayed, sessionId, userId);
            }
            if (tokenRevoked || sessionRevoked || tokenExpiry <= now.UtcDateTime || sessionExpiry <= now.UtcDateTime || replacementExpiry <= now)
            {
                await tx.CommitAsync(cancellationToken);
                return new RefreshRotationResult(RefreshRotationStatus.Invalid, null, null);
            }
            var effectiveExpiry = replacementExpiry.UtcDateTime < sessionExpiry ? replacementExpiry : new DateTimeOffset(sessionExpiry); 
            await using var insert = new SqlCommand(@"INSERT INTO [identity].[RefreshToken](SessionId,TokenHash,FamilyId,ExpiresAt)
OUTPUT INSERTED.RefreshTokenId VALUES(@sid,@newhash,@family,@expiry);", connection, tx);
            insert.Parameters.Add("@sid", SqlDbType.UniqueIdentifier).Value = sessionId;
            insert.Parameters.Add("@newhash", SqlDbType.VarBinary, 32).Value = replacementTokenHash;
            insert.Parameters.Add("@family", SqlDbType.UniqueIdentifier).Value = familyId;
            insert.Parameters.Add("@expiry", SqlDbType.DateTime2).Value = effectiveExpiry.UtcDateTime;
            var newId = (long)(await insert.ExecuteScalarAsync(cancellationToken) ?? throw new InvalidOperationException("Missing token id."));
            await using var update = new SqlCommand(@"UPDATE [identity].[RefreshToken] SET UsedAt=@now,ReplacedByTokenId=@newId
WHERE RefreshTokenId=@oldId AND UsedAt IS NULL;", connection, tx);
            update.Parameters.Add("@now", SqlDbType.DateTime2).Value = now.UtcDateTime;
            update.Parameters.Add("@newId", SqlDbType.BigInt).Value = newId;
            update.Parameters.Add("@oldId", SqlDbType.BigInt).Value = tokenId;
            if (await update.ExecuteNonQueryAsync(cancellationToken) != 1) throw new DBConcurrencyException("Refresh token rotation conflict.");
            await tx.CommitAsync(cancellationToken);
            return new RefreshRotationResult(RefreshRotationStatus.Success, sessionId, userId, effectiveExpiry);
        }
        catch { await tx.RollbackAsync(CancellationToken.None); throw; }
    }

    public async Task RevokeAsync(Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty) throw new ArgumentException("Session ID required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            await using var command = new SqlCommand(@"UPDATE [identity].[UserSession] SET RevokedAt=COALESCE(RevokedAt,@now) WHERE SessionId=@sid;
UPDATE [identity].[RefreshToken] SET RevokedAt=COALESCE(RevokedAt,@now) WHERE SessionId=@sid;", connection, tx);
            command.Parameters.Add("@sid", SqlDbType.UniqueIdentifier).Value = sessionId;
            command.Parameters.Add("@now", SqlDbType.DateTime2).Value = now.UtcDateTime;
            await command.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch { await tx.RollbackAsync(CancellationToken.None); throw; }
    }
}
