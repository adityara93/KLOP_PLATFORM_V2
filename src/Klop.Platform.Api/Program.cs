using Klop.Platform.Infrastructure;
using Klop.Platform.Api;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure();
builder.Services.AddPlatformAuthentication(builder.Configuration);
builder.Services.AddPlatformJwt(builder.Configuration);
var app = builder.Build();
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var supplied) &&
        supplied.Count == 1 && Guid.TryParse(supplied.ToString(), out var parsed)
        ? parsed.ToString("D") : Guid.NewGuid().ToString("D");
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    using (app.Logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
    {
        await next(context);
    }
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }))
    .WithName("Liveness").AllowAnonymous();
app.MapPlatformAuth();
app.Run();

public partial class Program { }
