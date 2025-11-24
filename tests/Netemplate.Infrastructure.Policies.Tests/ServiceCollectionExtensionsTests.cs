using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Netemplate.Infrastructure.Policies.Config;

namespace Netemplate.Infrastructure.Policies.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddResiliencePolicies_RegistersIResiliencePolicyRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Resilience:Cache:RetryCount"] = "3",
                ["Resilience:Cache:BaseRetryDelayMs"] = "200",
                ["Resilience:Cache:TimeoutSeconds"] = "5",
                ["Resilience:Cache:BulkheadMaxConcurrency"] = "50",
                ["Resilience:Cache:BulkheadQueueLimit"] = "200",
                ["Resilience:Messaging:RetryCount"] = "3",
                ["Resilience:Messaging:BaseRetryDelayMs"] = "300",
                ["Resilience:Messaging:TimeoutSeconds"] = "10",
                ["Resilience:Messaging:CircuitBreakerFailures"] = "5",
                ["Resilience:Messaging:CircuitBreakerDurationSeconds"] = "30",
                ["Resilience:Messaging:BulkheadMaxConcurrency"] = "20",
                ["Resilience:Messaging:BulkheadQueueLimit"] = "200"
            })
            .Build();

        // Act
        services.AddResiliencePolicies(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var registry = serviceProvider.GetService<IResiliencePolicyRegistry>();
        registry.Should().NotBeNull();
        registry!.Cache.Should().NotBeNull();
        registry.Messaging.Should().NotBeNull();
    }

    [Fact]
    public void AddResiliencePolicies_RegistersAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Resilience:Cache:RetryCount"] = "3",
                ["Resilience:Cache:BaseRetryDelayMs"] = "200",
                ["Resilience:Cache:TimeoutSeconds"] = "5",
                ["Resilience:Cache:BulkheadMaxConcurrency"] = "50",
                ["Resilience:Cache:BulkheadQueueLimit"] = "200",
                ["Resilience:Messaging:RetryCount"] = "3",
                ["Resilience:Messaging:BaseRetryDelayMs"] = "300",
                ["Resilience:Messaging:TimeoutSeconds"] = "10",
                ["Resilience:Messaging:CircuitBreakerFailures"] = "5",
                ["Resilience:Messaging:CircuitBreakerDurationSeconds"] = "30",
                ["Resilience:Messaging:BulkheadMaxConcurrency"] = "20",
                ["Resilience:Messaging:BulkheadQueueLimit"] = "200"
            })
            .Build();

        // Act
        services.AddResiliencePolicies(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var registry1 = serviceProvider.GetService<IResiliencePolicyRegistry>();
        var registry2 = serviceProvider.GetService<IResiliencePolicyRegistry>();
        
        registry1.Should().BeSameAs(registry2);
    }

    [Fact]
    public void AddResiliencePolicies_WithCustomConfiguration_UsesCustomValues()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Resilience:Cache:RetryCount"] = "7",
                ["Resilience:Cache:BaseRetryDelayMs"] = "500",
                ["Resilience:Cache:TimeoutSeconds"] = "15",
                ["Resilience:Cache:BulkheadMaxConcurrency"] = "100",
                ["Resilience:Cache:BulkheadQueueLimit"] = "500",
                ["Resilience:Messaging:RetryCount"] = "5",
                ["Resilience:Messaging:BaseRetryDelayMs"] = "1000",
                ["Resilience:Messaging:TimeoutSeconds"] = "30",
                ["Resilience:Messaging:CircuitBreakerFailures"] = "10",
                ["Resilience:Messaging:CircuitBreakerDurationSeconds"] = "60",
                ["Resilience:Messaging:BulkheadMaxConcurrency"] = "50",
                ["Resilience:Messaging:BulkheadQueueLimit"] = "300"
            })
            .Build();

        // Act
        services.AddResiliencePolicies(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var registry = serviceProvider.GetRequiredService<IResiliencePolicyRegistry>();
        registry.Cache.Retry.Should().NotBeNull();
        registry.Messaging.CircuitBreaker.Should().NotBeNull();
    }
}
