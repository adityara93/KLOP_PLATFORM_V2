using Klop.Platform.Application.Identity;
using Klop.Platform.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
namespace Klop.Platform.Infrastructure;
public static class AuthenticationRegistration
{
 public static IServiceCollection AddPlatformAuthentication(this IServiceCollection services,IConfiguration config)
 {
  var cs=config.GetConnectionString("PlatformV2");
  services.AddSingleton<Microsoft.Extensions.Options.IOptions<JwtOptions>>(_ =>
  {
   var options=config.GetSection("Jwt").Get<JwtOptions>()??throw new InvalidOperationException("Missing JWT settings.");
   if(string.IsNullOrWhiteSpace(options.Issuer)||string.IsNullOrWhiteSpace(options.Audience)||
      System.Text.Encoding.UTF8.GetByteCount(options.SigningKey??"")<32||options.AccessTokenMinutes is <1 or >60)
      throw new InvalidOperationException("Invalid JWT settings.");
   return Options.Create(options);
  });
  services.AddSingleton(TimeProvider.System);
  services.AddSingleton<IPasswordHasher,Pbkdf2PasswordHasher>();
  services.AddSingleton<IRefreshTokenGenerator,SecureRefreshTokenGenerator>();
  services.AddSingleton<IAccessTokenIssuer,HmacAccessTokenIssuer>();
  services.AddScoped<IUserSessionStore>(_=>new SqlUserSessionStore(cs??throw new InvalidOperationException("Missing PlatformV2 connection.")));
  services.AddScoped<SqlAuthenticationService>(sp=>new SqlAuthenticationService(cs??throw new InvalidOperationException("Missing PlatformV2 connection."),sp.GetRequiredService<IPasswordHasher>(),
   sp.GetRequiredService<IRefreshTokenGenerator>(),sp.GetRequiredService<IAccessTokenIssuer>(),sp.GetRequiredService<TimeProvider>()));
  return services;
 }
}
