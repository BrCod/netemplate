using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Netemplate.Application.Interfaces;
using Netemplate.Infrastructure.Cache.Redis;

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

            // Remove cache registrations
            var icacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ICache));
            if (icacheDescriptor != null) services.Remove(icacheDescriptor);

            // Remove message bus registrations
            var imessageDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMessageBus));
            if (imessageDescriptor != null) services.Remove(imessageDescriptor);
            // Remove event publisher registrations
            var ieventDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEventPublisher));
            if (ieventDescriptor != null) services.Remove(ieventDescriptor);


            // Register in-memory replacements
            services.AddSingleton<ICache, InMemoryCache>();
            services.AddSingleton<IMessageBus, InMemoryMessageBus>();
            services.AddScoped<IEventPublisher, InMemoryEventPublisher>();
        });

        base.ConfigureWebHost(builder);
    }
}
