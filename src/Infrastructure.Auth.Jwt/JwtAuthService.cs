using Netemplate.Application.Interfaces;

namespace Netemplate.Infrastructure.Auth.Jwt;

public sealed class JwtAuthService : IAuthService
{
    // Placeholder implementation - full JWT validation will be configured in API layer
    public Task<bool> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        // Token validation is handled by ASP.NET Core JWT middleware
        // This service can be extended for custom validation logic if needed
        return Task.FromResult(!string.IsNullOrWhiteSpace(token));
    }
}
