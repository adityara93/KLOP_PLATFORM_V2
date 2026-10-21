namespace Klop.Platform.Application.Identity;

/// <summary>Trusted, database-validated identity. Never construct directly from user-supplied login suffix.</summary>
public sealed record AccessTokenPrincipal(long UserId, int? TenantId, string Scope, Guid SessionId);
public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAt);
public interface IAccessTokenIssuer { IssuedAccessToken Issue(AccessTokenPrincipal principal); }
