namespace Netemplate.Application.Interfaces;

public interface IAuthService
{
    Task<bool> ValidateTokenAsync(string token, CancellationToken ct = default);
}
