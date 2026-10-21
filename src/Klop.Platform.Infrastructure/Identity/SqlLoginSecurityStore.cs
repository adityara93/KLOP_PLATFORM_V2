using System.Data;
using Klop.Platform.Application.Identity;
using Microsoft.Data.SqlClient;

namespace Klop.Platform.Infrastructure.Identity;

/// <summary>Atomic credential lockout state changes and audit records.</summary>
public sealed class SqlLoginSecurityStore(string connectionString) : ILoginSecurityStore
{
    private const int MaximumAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<LoginFailureResult> RecordFailureAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            await using var command = new SqlCommand(@"
UPDATE c WITH (UPDLOCK)
SET FailedLoginCount = CASE WHEN c.LockoutEndAt IS NOT NULL AND c.LockoutEndAt <= @now THEN 1
                            WHEN c.FailedLoginCount >= @max THEN @max ELSE c.FailedLoginCount + 1 END,
    LockoutEndAt = CASE WHEN (c.LockoutEndAt IS NOT NULL AND c.LockoutEndAt > @now) THEN c.LockoutEndAt
                        WHEN (CASE WHEN c.LockoutEndAt IS NOT NULL AND c.LockoutEndAt <= @now THEN 1 ELSE c.FailedLoginCount + 1 END) >= @max THEN @until
                        ELSE NULL END
OUTPUT INSERTED.FailedLoginCount, INSERTED.LockoutEndAt
FROM [identity].[UserCredential] c WHERE c.UserId=@uid;", connection, tx);
            command.Parameters.Add("@uid", SqlDbType.BigInt).Value = userId;
            command.Parameters.Add("@now", SqlDbType.DateTime2).Value = now.UtcDateTime;
            command.Parameters.Add("@until", SqlDbType.DateTime2).Value = now.Add(LockoutDuration).UtcDateTime;
            command.Parameters.Add("@max", SqlDbType.Int).Value = MaximumAttempts;
            int attempts;
            DateTimeOffset? lockedUntil;
            await using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken))
            {
                if (!await reader.ReadAsync(cancellationToken)) throw new InvalidOperationException("Credential not found.");
                attempts = reader.GetInt32(0);
                lockedUntil = reader.IsDBNull(1) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc));
            }
            await using var audit = new SqlCommand(@"
INSERT INTO [audit].[AuditLog](ActorUserId,ActorTenantId,TargetTenantId,ResourceType,ResourceId,Action,Outcome,OccurredAt)
SELECT u.UserId,u.TenantId,u.TenantId,'IDENTITY',CONVERT(nvarchar(150),u.UserId),'LOGIN','FAILED',@now
FROM [identity].[User] u WHERE u.UserId=@uid;", connection, tx);
            audit.Parameters.Add("@uid", SqlDbType.BigInt).Value = userId;
            audit.Parameters.Add("@now", SqlDbType.DateTime2).Value = now.UtcDateTime;
            await audit.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return new LoginFailureResult(attempts, lockedUntil);
        }
        catch { await tx.RollbackAsync(CancellationToken.None); throw; }
    }

    public async Task RecordSuccessAsync(long userId, int? tenantId, DateTimeOffset now, Guid? correlationId = null, CancellationToken cancellationToken = default)
    {
        if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            await using var command = new SqlCommand(@"
UPDATE [identity].[UserCredential] SET FailedLoginCount=0,LockoutEndAt=NULL WHERE UserId=@uid;
IF @@ROWCOUNT <> 1 THROW 51000, 'Credential not found', 1;
INSERT INTO [audit].[AuditLog](ActorUserId,ActorTenantId,TargetTenantId,ResourceType,ResourceId,Action,Outcome,OccurredAt,CorrelationId)
VALUES(@uid,@tenant,@tenant,'IDENTITY',CONVERT(nvarchar(150),@uid),'LOGIN','SUCCESS',@now,@correlation);", connection, tx);
            command.Parameters.Add("@uid", SqlDbType.BigInt).Value = userId;
            command.Parameters.Add("@tenant", SqlDbType.Int).Value = (object?)tenantId ?? DBNull.Value;
            command.Parameters.Add("@now", SqlDbType.DateTime2).Value = now.UtcDateTime;
            command.Parameters.Add("@correlation", SqlDbType.UniqueIdentifier).Value = (object?)correlationId ?? DBNull.Value;
            await command.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch { await tx.RollbackAsync(CancellationToken.None); throw; }
    }
}
