using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Klop.Platform.Application.Identity;
using Klop.Platform.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

namespace Klop.Platform.Api;

public static class AuthenticationEndpoints
{
    public static IServiceCollection AddPlatformJwt(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options=>
        {
            var jwt=config.GetSection("Jwt").Get<JwtOptions>() ?? throw new InvalidOperationException("JWT configuration required.");
            var signingKey=jwt.SigningKey;
            if(string.IsNullOrWhiteSpace(jwt.Issuer)||string.IsNullOrWhiteSpace(jwt.Audience)||
               string.IsNullOrWhiteSpace(signingKey)||Encoding.UTF8.GetByteCount(signingKey)<32||
               jwt.AccessTokenMinutes is <1 or >60)
               throw new InvalidOperationException("Invalid JWT configuration.");
            options.MapInboundClaims=false;
            options.TokenValidationParameters=new TokenValidationParameters
            {
                ValidateIssuer=true,ValidIssuer=jwt.Issuer,ValidateAudience=true,ValidAudience=jwt.Audience,
                ValidateIssuerSigningKey=true,IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ValidateLifetime=true,RequireExpirationTime=true,RequireSignedTokens=true,
                ClockSkew=TimeSpan.FromSeconds(30)
            };
            options.Events=new JwtBearerEvents
            {
                OnTokenValidated=async ctx=>
                {
                    var p=ctx.Principal;
                    if(p is null||!long.TryParse(p.FindFirst("sub")?.Value,out var uid)||
                       !Guid.TryParse(p.FindFirst("sid")?.Value,out var sid))
                    {ctx.Fail("Invalid session.");return;}
                    var scope=p.FindFirst("scope")?.Value;
                    int? tenant=null;
                    var tenantValue=p.FindFirst("tenant_id")?.Value;
                    if(tenantValue is not null)
                    {
                        if(!int.TryParse(tenantValue,out var value)){ctx.Fail("Invalid tenant.");return;}
                        tenant=value;
                    }
                    if(scope is not ("PLATFORM" or "TENANT")||
                       (scope=="PLATFORM"&&tenant is not null)||(scope=="TENANT"&&tenant is null or <=0))
                    {ctx.Fail("Invalid scope.");return;}
                    var auth=ctx.HttpContext.RequestServices.GetRequiredService<SqlAuthenticationService>();
                    if(!await auth.IsSessionActiveAsync(uid,sid,scope,tenant,ctx.HttpContext.RequestAborted))
                        ctx.Fail("Inactive session.");
                }
            };
        });
        services.AddAuthorization();
        services.AddRateLimiter(options=>
        {
            options.RejectionStatusCode=429;
            options.AddPolicy("auth",ctx=>RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString()??"unknown",
                _=>new FixedWindowRateLimiterOptions{PermitLimit=10,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
        });
        return services;
    }

    public static void MapPlatformAuth(this WebApplication app)
    {
        var group=app.MapGroup("/api/auth");
        group.MapPost("/login",async (LoginBody request,SqlAuthenticationService auth,HttpContext ctx,CancellationToken ct)=>
        {
            var result=await auth.LoginAsync(request.Username,request.Password,
                ctx.Connection.RemoteIpAddress?.ToString(),ctx.Request.Headers.UserAgent.ToString(),ct);
            return result is null?Results.Unauthorized():Results.Ok(result);
        }).RequireRateLimiting("auth").AllowAnonymous();

        group.MapPost("/refresh",async (RefreshBody request,SqlAuthenticationService auth,IUserSessionStore store,CancellationToken ct)=>
        {
            var result=await auth.RefreshAsync(request.RefreshToken,store,ct);
            return result is null?Results.Unauthorized():Results.Ok(result);
        }).RequireRateLimiting("auth").AllowAnonymous();

        group.MapPost("/logout",async (ClaimsPrincipal principal,IUserSessionStore store,CancellationToken ct)=>
        {
            if(!Guid.TryParse(principal.FindFirst("sid")?.Value,out var sid)) return Results.Unauthorized();
            await store.RevokeAsync(sid,DateTimeOffset.UtcNow,ct);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapGet("/me",(ClaimsPrincipal principal)=>Results.Ok(new
        {
            userId=principal.FindFirst("sub")?.Value,
            tenantId=principal.FindFirst("tenant_id")?.Value,
            scope=principal.FindFirst("scope")?.Value
        })).RequireAuthorization();
    }
}
public sealed record LoginBody(string Username,string Password);
public sealed record RefreshBody(string RefreshToken);
