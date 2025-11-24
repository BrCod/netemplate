using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Netemplate.Infrastructure.Policies.Config;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResiliencePolicies(this IServiceCollection services, IConfiguration config)
    {
        var options = new ResilienceOptions();
        config.GetSection(ResilienceOptions.SectionName).Bind(options);
        var registry = ResiliencePolicyRegistry.Create(options);
        services.AddSingleton<IResiliencePolicyRegistry>(registry);
        services.AddSingleton(options);
        return services;
    }
}
