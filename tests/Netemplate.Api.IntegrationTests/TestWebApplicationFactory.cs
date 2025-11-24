using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text.Encodings.Web;
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
                ["ConnectionStrings:RabbitMQ"] = "amqp://guest:guest@localhost:5672",
                ["DeadLetterQueue:Enabled"] = "false",
                ["RateLimiting:PermitLimit"] = "100",
                ["RateLimiting:Window"] = "00:01:00",
                ["RateLimiting:SegmentsPerWindow"] = "1"
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

            // Remove dead-letter queue handler to avoid RabbitMQ connection attempts
            var dlqHandlerDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Netemplate.Infrastructure.Messaging.RabbitMq.DeadLetter.DeadLetterQueueHandler));
            if (dlqHandlerDescriptor != null) services.Remove(dlqHandlerDescriptor);

            var dlqOptionsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Netemplate.Infrastructure.Messaging.RabbitMq.DeadLetter.DeadLetterQueueOptions));
            if (dlqOptionsDescriptor != null) services.Remove(dlqOptionsDescriptor);

            var dlqAlertDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Netemplate.Infrastructure.Messaging.RabbitMq.DeadLetter.IDeadLetterAlertService));
            if (dlqAlertDescriptor != null) services.Remove(dlqAlertDescriptor);

            // Register in-memory replacements
            services.AddSingleton<ICache, InMemoryCache>();
            services.AddSingleton<IMessageBus, InMemoryMessageBus>();
            services.AddScoped<IEventPublisher, InMemoryEventPublisher>();
        });

        builder.ConfigureTestServices(services =>
        {
            // Override authentication with test authentication that auto-succeeds
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestAuth";
                options.DefaultChallengeScheme = "TestAuth";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestAuth", options => { });
        });

        base.ConfigureWebHost(builder);
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "TestUser") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestAuth");
        
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
