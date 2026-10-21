namespace Klop.Platform.Application.Identity;

/// <summary>Login request has exactly the two public fields required by the V2 contract.</summary>
public sealed record LoginRequest(string Username, string Password);
