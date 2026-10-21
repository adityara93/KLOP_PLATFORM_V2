using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Klop.Platform.Application.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Klop.Platform.Infrastructure.Identity;

public sealed class HmacAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _time;
    public HmacAccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider time)
    {
        _options = options.Value;
        _time = time;
        ValidateOptions(_options);
    }
    public IssuedAccessToken Issue(AccessTokenPrincipal principal)
    {
        if (principal.UserId <= 0 || principal.SessionId == Guid.Empty) throw new ArgumentException("Invalid trusted principal.");
        if (principal.Scope == "PLATFORM" && principal.TenantId is not null) throw new ArgumentException("PLATFORM identity must not have tenant.");
        if (principal.Scope == "TENANT" && (principal.TenantId is null or <= 0)) throw new ArgumentException("TENANT identity must have tenant.");
        if (principal.Scope is not ("PLATFORM" or "TENANT")) throw new ArgumentException("Invalid identity scope.");
        var now = _time.GetUtcNow();
        var expiry = now.AddMinutes(_options.AccessTokenMinutes);
        var claims = new List<Claim> {
            new(JwtRegisteredClaimNames.Sub, principal.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("D")),
            new("sid", principal.SessionId.ToString("D")),
            new("scope", principal.Scope)
        };
        if (principal.TenantId is int tenantId) claims.Add(new Claim("tenant_id", tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, now.UtcDateTime, expiry.UtcDateTime, credentials);
        return new IssuedAccessToken(new JwtSecurityTokenHandler().WriteToken(jwt), expiry);
    }
    private static void ValidateOptions(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer) || string.IsNullOrWhiteSpace(options.Audience) ||
            string.IsNullOrWhiteSpace(options.SigningKey) || Encoding.UTF8.GetByteCount(options.SigningKey) < 32 ||
            options.AccessTokenMinutes is < 1 or > 60)
            throw new InvalidOperationException("Invalid JWT configuration; issuer, audience, >=256-bit secret and 1-60 minute lifetime required.");
    }
}
