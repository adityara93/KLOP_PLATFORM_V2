namespace Klop.Platform.Application.Identity;

public sealed record LoginResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, long UserId, int? TenantId, string Scope);
