using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Netemplate.Application.Interfaces;
using Netemplate.Infrastructure.Cache.Redis;
using Netemplate.Infrastructure.Messaging.RabbitMq;

namespace Netemplate.Api.IntegrationTests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove Redis ConnectionMultiplexer if registered
            var multiplexerDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
            if (multiplexerDescriptor != null) services.Remove(multiplexerDescriptor);

            // Remove RedisCache registration
            var redisCacheDescriptor = services.FirstOrDefault(d => d.ImplementationType == typeof(RedisCache) || d.ServiceType == typeof(RedisCache));
            if (redisCacheDescriptor != null) services.Remove(redisCacheDescriptor);

            // Remove ICache registration
            var icacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ICache));
            if (icacheDescriptor != null) services.Remove(icacheDescriptor);

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
