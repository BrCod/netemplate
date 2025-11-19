namespace Netemplate.Api.Configuration;

public sealed class SecretsOptions
{
    public const string SectionName = "Secrets";

    public string Provider { get; set; } = "Environment"; // Environment, AzureKeyVault, HashiCorpVault
    public string? VaultUri { get; set; }
    public bool EnableRotation { get; set; } = false;
    public TimeSpan RotationCheckInterval { get; set; } = TimeSpan.FromHours(1);
}

public interface ISecretProvider
{
    Task<string?> GetSecretAsync(string key, CancellationToken ct = default);
    Task<bool> RefreshAsync(CancellationToken ct = default);
}

public sealed class EnvironmentSecretProvider : ISecretProvider
{
    private readonly ILogger<EnvironmentSecretProvider> _logger;

    public EnvironmentSecretProvider(ILogger<EnvironmentSecretProvider> logger)
    {
        _logger = logger;
    }

    public Task<string?> GetSecretAsync(string key, CancellationToken ct = default)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return Task.FromResult(value);
    }

    public Task<bool> RefreshAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Environment secrets refreshed");
        return Task.FromResult(true);
    }
}
