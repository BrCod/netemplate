# netemplate Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-11-13

## Active Technologies

- .NET 8 (C# 12) + ASP.NET Core, Swashbuckle.AspNetCore, Microsoft.AspNetCore.Authentication.JwtBearer, Microsoft.AspNetCore.RateLimiting, EF Core (Npgsql provider), StackExchange.Redis, RabbitMQ.Client, OpenTelemetry.Extensions.Hosting (optional), Microsoft.Extensions.Logging, HealthChecks packages, NWebsec (optional) (001-clean-arch-api)
	- Polly for resilience (retry, circuit breaker, timeout, bulkhead)
	- Outbox pattern for reliable messaging
	- Feature flags (configurable via environment/appsettings)
	- OpenTelemetry tracing and metrics (required)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for .NET 8 (C# 12)

## Code Style

.NET 8 (C# 12): Follow standard conventions

## Recent Changes

- 001-clean-arch-api: Added .NET 8 (C# 12) + ASP.NET Core, Swashbuckle.AspNetCore, Microsoft.AspNetCore.Authentication.JwtBearer, Microsoft.AspNetCore.RateLimiting, EF Core (Npgsql provider), StackExchange.Redis, RabbitMQ.Client, OpenTelemetry.Extensions.Hosting (optional), Microsoft.Extensions.Logging, HealthChecks packages, NWebsec (optional)
	- v1.1.0: Added resilience policies (Polly), outbox pattern, feature flags, mandatory OpenTelemetry tracing/metrics, and updated all planning artifacts accordingly.

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
