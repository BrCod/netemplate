using Application.Interfaces;
using Infrastructure.Postgres;
using Infrastructure.Postgres.Repositories;
using Infrastructure.Redis;
using Infrastructure.RabbitMq;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Api.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using StackExchange.Redis;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

// Configure logging with console output
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
});
builder.Logging.AddFilter((category, level) => level >= LogLevel.Information);

// Add services to the container
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IRepository<Domain.Entities.Product>, ProductRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<Application.Services.ProductService>();

// Redis configuration
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));

builder.Services.AddSingleton<ICache, RedisCacheAdapter>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqPublisher>();
builder.Services.AddSingleton<IAuthService, JwtAuthService>();

// OpenTelemetry configuration
Api.Observability.OpenTelemetryConfig.Configure(builder.Services);

// FluentValidation registration
builder.Services.AddValidatorsFromAssemblyContaining<Application.Validators.ProductCreateValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// JWT Authentication configuration
var jwtSection = builder.Configuration.GetSection("JWT");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSection["Secret"] ?? "YourSuperSecretKey")),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// Rate Limiting configuration (fixed window per IP)
var rateSection = builder.Configuration.GetSection("RateLimiting");
builder.Services.AddRateLimiter(options =>
{
    int permitLimit = int.TryParse(rateSection["PermitLimit"], out var p) ? p : 100;
    int windowSeconds = int.TryParse(rateSection["WindowSeconds"], out var w) ? w : 60;
    int queueLimit = int.TryParse(rateSection["QueueLimit"], out var q) ? q : 0;
    string policyName = rateSection["PolicyName"] ?? "default";

    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = queueLimit,
                AutoReplenishment = true
            }
        )
    );

    options.AddPolicy(policyName, ctx =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = queueLimit,
                AutoReplenishment = true
            }
        )
    );
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Suppress automatic 400 response; let ErrorHandlingMiddleware produce RFC 7807
        options.SuppressModelStateInvalidFilter = false;
    });
// TODO: Add API versioning library later; currently version encoded in route segment (/api/v1/)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter JWT with Bearer into field",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Register outbox dispatcher
builder.Services.AddScoped<Infrastructure.Postgres.Outbox.OutboxDispatcher>();
builder.Services.AddHostedService<OutboxDispatcherHostedService>();

// Register resilience policy provider
builder.Services.AddScoped<Infrastructure.Policies.ResiliencePolicyProvider>();

var app = builder.Build();

// Middlewares
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<ValidationMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
