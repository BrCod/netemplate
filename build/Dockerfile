# syntax=docker/dockerfile:1

# -------- Build stage --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for better layer caching
COPY *.sln ./
COPY src/Api/*.csproj ./src/Api/
COPY src/Application/*.csproj ./src/Application/
COPY src/Domain/*.csproj ./src/Domain/
COPY src/Infrastructure/*.csproj ./src/Infrastructure/

# Restore dependencies (cached unless project files change)
RUN dotnet restore Netemplate.sln

# Copy source code
COPY src/ ./src/

# Build and publish
RUN dotnet publish ./src/Api/Netemplate.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    /p:TreatWarningsAsErrors=true

# -------- Runtime stage --------
# Consider using chiseled image for smaller size and better security:
# FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Security: Run as non-root user (create early)
RUN adduser --disabled-password --gecos '' --uid 1000 appuser

WORKDIR /app

# App variables
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0 \
    OTEL_SERVICE_NAME=netemplate-api \
    OTEL_RESOURCE_ATTRIBUTES=deployment.environment=production \
    OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf \
    # Security headers
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Copy published output with correct ownership
COPY --from=build --chown=appuser:appuser /app/publish .

# Switch to non-root user
USER appuser

EXPOSE 8080

# Use built-in health checks (no external dependencies needed)
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 \
    CMD dotnet Netemplate.Api.dll --health-check || exit 1

ENTRYPOINT ["dotnet", "Netemplate.Api.dll"]
