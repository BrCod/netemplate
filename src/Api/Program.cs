using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using System.Threading.RateLimiting;
using Netemplate.Application.Interfaces;
using Netemplate.Infrastructure.Persistence.Postgres;
using Netemplate.Infrastructure.Persistence.Postgres.Outbox;
using Netemplate.Infrastructure.Cache.Redis;
using Netemplate.Infrastructure.Messaging.RabbitMq;
using Netemplate.Api.Middleware.Localization;
using Netemplate.Infrastructure.Messaging.RabbitMq.DeadLetter;
using Netemplate.Infrastructure.Policies.Config;
using Netemplate.Infrastructure.Auth.Jwt;
using Netemplate.Api.Middleware;
using Netemplate.Api.Logging;
using Netemplate.Api.Observability;
using Netemplate.Api.Health;
using Netemplate.Api.Configuration;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using StackExchange.Redis;
using RabbitMQ.Client;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.HttpsPolicy;




var builder = WebApplication.CreateBuilder(args);

var isTesting = builder.Environment.IsEnvironment("Testing");

// Configuration options
builder.Services.Configure<SecurityPoliciesOptions>(builder.Configuration.GetSection(SecurityPoliciesOptions.SectionName));
builder.Services.Configure<SecretsOptions>(builder.Configuration.GetSection(SecretsOptions.SectionName));

// Secret management
builder.Services.AddSingleton<ISecretProvider, EnvironmentSecretProvider>();

// Security: HTTPS enforcement
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

// Security: Request size limits
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5_242_880; // 5MB
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 5_242_880; // 5MB
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=netemplate;Username=postgres;Password=postgres";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Application Services
builder.Services.AddScoped<Netemplate.Application.Services.IProductService, Netemplate.Application.Services.ProductService>();

// Resilience policies (centralized)
builder.Services.AddResiliencePolicies(builder.Configuration);

// Cache (resilient) - Skip external Redis in Testing environment
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
if (!isTesting)
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var connString = config.GetConnectionString("Redis") ?? "localhost:6379";
        return ConnectionMultiplexer.Connect(connString);
    });
    builder.Services.AddSingleton<RedisCache>();
    builder.Services.AddSingleton<ICache>(sp => new ResilientCache(sp.GetRequiredService<RedisCache>(), sp.GetRequiredService<IResiliencePolicyRegistry>()));
}

// Messaging (resilient) - Skip external RabbitMQ in Testing environment
var rabbitMqConnection = builder.Configuration.GetConnectionString("RabbitMQ") ?? "amqp://guest:guest@localhost:5672";
if (!isTesting)
{
    builder.Services.AddSingleton<IConnectionFactory>(new ConnectionFactory { Uri = new Uri(rabbitMqConnection) });
    builder.Services.AddSingleton<RabbitMqMessageBus>();
    builder.Services.AddSingleton<IMessageBus>(sp => new ResilientMessageBus(sp.GetRequiredService<RabbitMqMessageBus>(), sp.GetRequiredService<IResiliencePolicyRegistry>()));
// Event Publisher
builder.Services.AddScoped<IEventPublisher, Netemplate.Infrastructure.Messaging.RabbitMq.EventPublisher>();

}

// Dead-letter queue
builder.Services.AddDeadLetterQueue(builder.Configuration);

// Auth
builder.Services.AddSingleton<IAuthService, JwtAuthService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "ThisIsASecretKeyForDevelopmentOnly1234567890";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "netemplate";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "netemplate-api";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:3000" };
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Rate Limiting
var rateLimitConfig = builder.Configuration.GetSection("RateLimiting");
var permitLimit = rateLimitConfig.GetValue<int>("PermitLimit", 100);
var window = rateLimitConfig.GetValue<TimeSpan>("Window", TimeSpan.FromMinutes(1));
var queueLimit = rateLimitConfig.GetValue<int>("QueueLimit", 10);
var segmentsPerWindow = rateLimitConfig.GetValue<int>("SegmentsPerWindow", 1);

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = queueLimit,
                SegmentsPerWindow = segmentsPerWindow,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc6585#section-4",
            title = "Too Many Requests",
            status = 429,
            detail = "Rate limit exceeded. Please try again later.",
            traceId = context.HttpContext.TraceIdentifier
        }, cancellationToken);
    };
});

// Exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// OpenTelemetry with adaptive sampling
var samplingRatio = builder.Configuration.GetValue<double>("OpenTelemetry:SamplingRatio", 0.1);
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("netemplate-api", serviceVersion: "1.0.0")
        .AddAttributes(new Dictionary<string, object>
        {
            ["environment"] = builder.Environment.EnvironmentName,
            ["host.name"] = Environment.MachineName
        }))
    .WithTracing(tracing => tracing
        .SetSampler(new AdaptiveSampler(samplingRatio))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

// Feature Flags
builder.Services.AddSingleton<Netemplate.Application.Services.IFeatureFlagService, Netemplate.Application.Services.InMemoryFeatureFlagService>();

// Schema Registry
builder.Services.AddSingleton<Netemplate.Application.Messaging.SchemaRegistry.IEventSchemaRegistry, Netemplate.Application.Messaging.SchemaRegistry.InMemoryEventSchemaRegistry>();

// Culture Formatters
builder.Services.AddSingleton<Netemplate.Application.Localization.Formatters.CultureFormatter>();
builder.Services.AddScoped<Netemplate.Application.Localization.Formatters.ICultureFormatter, Netemplate.Application.Localization.Formatters.CurrentCultureFormatter>();

// Outbox Dispatcher - avoid starting background service in Testing
if (!isTesting)
{
    builder.Services.AddHostedService<OutboxDispatcher>();
}

// Health checks
builder.Services.AddSingleton<ApplicationReadinessCheck>();
var hcBuilder = builder.Services.AddHealthChecks()
    .AddCheck<ApplicationReadinessCheck>("app_readiness", tags: new[] { "readiness" })
    .AddNpgSql(connectionString, name: "postgres", tags: new[] { "db", "readiness" });

if (!isTesting)
{
    hcBuilder
        .AddRedis(redisConnection, name: "redis", tags: new[] { "cache", "readiness" })
        .AddRabbitMQ(rabbitMqConnection, name: "rabbitmq", tags: new[] { "messaging", "readiness" });
}

// Localization for problem+json responses
builder.Services.AddApiLocalization();

// Swagger Localization
builder.Services.AddSingleton<Netemplate.Api.Swagger.Localization.ISwaggerLocalizer, Netemplate.Api.Swagger.Localization.SwaggerLocalizer>();

// API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var swaggerLocalizer = new Netemplate.Api.Swagger.Localization.SwaggerLocalizer();
var supportedCultures = swaggerLocalizer.GetSupportedCultures();
var languageList = string.Join(", ", supportedCultures.Select(c => $"{c.DisplayName} ({c.Name})"));

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = swaggerLocalizer.GetTitle(),
        Version = "v1",
        Description = swaggerLocalizer.GetDescription() + 
            $"\n\n**Supported Languages:** {languageList}" +
            "\n\n**Localization:**" +
            "\n- Use `Accept-Language` header to specify preferred language" +
            "\n- Use `?culture=xx-XX` query parameter to override language" +
            "\n- Default language: English (en-US)" +
            "\n- Error messages and responses will be localized based on your preference",
        Contact = new OpenApiContact
        {
            Name = swaggerLocalizer.GetContactName(),
            Email = swaggerLocalizer.GetContactEmail()
        },
        License = new OpenApiLicense
        {
            Name = swaggerLocalizer.GetLicenseName()
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    //c.AddSecurityRequirement(new OpenApiSecurityRequirement
    //{
    //    {
    //        new OpenApiSecurityScheme
    //        {
    //            Reference = new OpenApiReference
    //            {
    //                Type = ReferenceType.SecurityScheme,
    //                Id = "Bearer"
    //            }
    //        },
    //        Array.Empty<string>()
    //    }
    //});

});


var app = builder.Build();

// Pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestSizeLimitMiddleware>();
app.UseMiddleware<ValidationMiddleware>();

// Security headers
app.UseMiddleware<SecurityHeadersMiddleware>();

// Localization (must come before exception handler to localize problem responses)
app.UseLocalization();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Graceful shutdown hook
var lifetime = app.Lifetime;
lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("Application is shutting down - flushing telemetry/logs");
});

// Controllers
app.MapControllers();

// Health endpoints
app.MapHealthChecks("/health/live", HealthCheckConfiguration.CreateLivenessOptions()).AllowAnonymous();
app.MapHealthChecks("/health/ready", HealthCheckConfiguration.CreateReadinessOptions()).AllowAnonymous();

// Initialize dead-letter queue infrastructure (skip in test environment or if disabled)
var dlqEnabled = app.Configuration.GetValue<bool>("DeadLetterQueue:Enabled", true);
if (dlqEnabled && !app.Environment.IsEnvironment("Testing"))
{
    try
    {
        await app.Services.InitializeDeadLetterQueueAsync();
    }
    catch (Exception ex)
    {
        // Log warning but don't fail startup if DLQ initialization fails
        app.Logger.LogWarning(ex, "Failed to initialize dead-letter queue. Application will continue without DLQ monitoring.");
    }
}

// Mark application as ready
var readinessCheck = app.Services.GetRequiredService<ApplicationReadinessCheck>();
readinessCheck.MarkAsReady();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// Make Program accessible for integration tests
public partial class Program { }
