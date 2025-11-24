using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Netemplate.Application.Interfaces;
using Netemplate.Infrastructure.Cache.Redis;
using Netemplate.Infrastructure.Messaging.RabbitMq;
using Netemplate.Infrastructure.Resilience;

namespace Netemplate.Api.IntegrationTests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test-specific configuration that disables external dependencies
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = "localhost:6379,abortConnect=false,connectTimeout=100,connectRetry=1",
                ["ConnectionStrings:RabbitMQ"] = "amqp://guest:guest@localhost:5672"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove Redis ConnectionMultiplexer if registered
            var multiplexerDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
            if (multiplexerDescriptor != null) services.Remove(multiplexerDescriptor);

            // Remove resilient cache wrapper
            var resilientCacheDescriptor = services.FirstOrDefault(d => d.ImplementationType == typeof(ResilientCache));
            if (resilientCacheDescriptor != null) services.Remove(resilientCacheDescriptor);

            // Remove RedisCache registration
            var redisCacheDescriptor = services.FirstOrDefault(d => d.ImplementationType == typeof(RedisCache) || d.ServiceType == typeof(RedisCache));
            if (redisCacheDescriptor != null) services.Remove(redisCacheDescriptor);

            // Remove ICache registration
            var icacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ICache));
            if (icacheDescriptor != null) services.Remove(icacheDescriptor);

            // Remove resilient message bus wrapper
            var resilientMessageBusDescriptor = services.FirstOrDefault(d => d.ImplementationType == typeof(ResilientMessageBus));
            if (resilientMessageBusDescriptor != null) services.Remove(resilientMessageBusDescriptor);

            // Remove RabbitMqMessageBus registration
            var rabbitDescriptor = services.FirstOrDefault(d => d.ImplementationType == typeof(RabbitMqMessageBus) || d.ServiceType == typeof(RabbitMqMessageBus));
            if (rabbitDescriptor != null) services.Remove(rabbitDescriptor);

            var imessageDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMessageBus));
            if (imessageDescriptor != null) services.Remove(imessageDescriptor);

            // Register in-memory replacements
            services.AddSingleton<ICache, InMemoryCache>();
            services.AddSingleton<IMessageBus, InMemoryMessageBus>();
        });

        base.ConfigureWebHost(builder);
    }
}
