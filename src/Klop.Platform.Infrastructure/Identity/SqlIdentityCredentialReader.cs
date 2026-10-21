using System.Data;
using Klop.Platform.Application.Identity;
using Microsoft.Data.SqlClient;

namespace Klop.Platform.Infrastructure.Identity;

/// <summary>Parameterized, read-only lookup. No connection is opened until the caller invokes FindAsync.</summary>
public sealed class SqlIdentityCredentialReader(string connectionString) : IIdentityCredentialReader
{
    public async Task<AuthenticationCandidate?> FindAsync(string normalizedUsername, string tenantCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(normalizedUsername) || string.IsNullOrWhiteSpace(tenantCode)) return null;
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT TOP (1) u.UserId, u.TenantId, u.Scope, c.PasswordHash, u.IsActive,
       CAST(COALESCE(t.IsActive, 1) AS bit) AS IsTenantActive, c.FailedLoginCount, c.LockoutEndAt
FROM [identity].[User] u
INNER JOIN [identity].[UserCredential] c ON c.UserId = u.UserId
LEFT JOIN [tenant].[Tenant] t ON t.TenantId = u.TenantId
WHERE u.NormalizedUsername = @username
  AND ((@tenantCode = 'PLATFORM' AND u.Scope = 'PLATFORM' AND u.TenantId IS NULL AND UPPER(u.Username) = 'ADMIN')
    OR (@tenantCode <> 'PLATFORM' AND u.Scope = 'TENANT' AND t.TenantCode = @tenantCode));";
        command.Parameters.Add("@username", SqlDbType.NVarChar, 100).Value = normalizedUsername;
        command.Parameters.Add("@tenantCode", SqlDbType.VarChar, 30).Value = tenantCode;
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new AuthenticationCandidate(
            reader.GetInt64(0), reader.IsDBNull(1) ? null : reader.GetInt32(1), reader.GetString(2),
            reader.GetString(3), reader.GetBoolean(4), reader.GetBoolean(5), reader.GetInt32(6),
            reader.IsDBNull(7) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc)));
    }
}
