# Smoke Tests

This project contains smoke tests for the Netemplate API. Smoke tests are high-level tests that validate the basic functionality and deployment of the application.

## Purpose

Smoke tests verify that:
- The application can start successfully
- Critical endpoints are accessible
- Authentication and authorization are working
- Health checks respond correctly
- Basic infrastructure is operational

These tests are designed to run quickly and catch major issues before running more comprehensive test suites.

## Test Categories

### Health Endpoint Tests (`HealthEndpointSmokeTests.cs`)

Validates health check endpoints:
- Liveness probe (`/health/live`)
- Readiness probe (`/health/ready`)
- Response times and consistency
- No authentication required for health checks

**Key Tests:**
- `HealthLive_ReturnsOk` - Liveness check returns 200 OK
- `HealthReady_ReturnsSuccessStatusCode` - Readiness check succeeds
- `HealthLive_ResponseTimeIsAcceptable` - Health checks are fast (<1s)
- `HealthChecks_DontRequireAuthentication` - Public access to health endpoints

### JWT Protected Endpoint Tests (`JwtProtectedEndpointSmokeTests.cs`)

Validates authentication and authorization:
- Protected endpoints require JWT tokens
- Invalid/expired tokens are rejected
- Unauthorized responses include correct headers
- Authentication is consistently enforced

**Key Tests:**
- `ProtectedEndpoint_WithoutToken_ReturnsUnauthorized` - 401 without token
- `ProtectedEndpoint_WithInvalidToken_ReturnsUnauthorized` - 401 with invalid token
- `ProtectedEndpoint_WithExpiredToken_ReturnsUnauthorized` - 401 with expired token
- `UnauthorizedResponse_ReturnsWwwAuthenticateHeader` - Proper auth challenge

### Basic API Tests (`BasicApiSmokeTests.cs`)

Validates general API functionality:
- Application startup and response
- Swagger documentation accessibility
- Error handling (404, 405)
- Security headers and CORS
- Concurrent request handling
- Rate limiting configuration

**Key Tests:**
- `Application_CanStart` - App starts and responds
- `Swagger_IsAccessible` - Documentation available
- `NotFoundEndpoint_ReturnsProblemDetails` - Proper error format
- `Application_RespondsToMultipleConcurrentRequests` - Concurrency handling
- `RateLimiting_IsConfigured` - Rate limiting active

## Running the Tests

### Using .NET CLI

```powershell
# Run all smoke tests
dotnet test tests/Netemplate.Api.Smoke/

# Run with verbose output
dotnet test tests/Netemplate.Api.Smoke/ --verbosity normal

# Run specific test class
dotnet test tests/Netemplate.Api.Smoke/ --filter "FullyQualifiedName~HealthEndpointSmokeTests"

# Run tests with category filter
dotnet test --filter "Category=Smoke"
```

### Using PowerShell Scripts

```powershell
# From build directory
.\test.ps1 -Filter "Category=Smoke"
```

### In CI/CD Pipeline

```yaml
- name: Run smoke tests
  run: dotnet test --filter "Category=Smoke" --logger "trx"
```

## Test Characteristics

### Fast Execution
- Target: Complete suite in < 30 seconds
- Individual tests: < 5 seconds each
- Parallel execution supported

### No External Dependencies
- Uses `WebApplicationFactory` for in-process testing
- In-memory implementations for infrastructure
- No real database/Redis/RabbitMQ required

### High-Level Validation
- Tests behavior, not implementation
- Focuses on critical paths
- Validates deployment readiness

## Expected Results

All smoke tests should pass before:
- Deploying to any environment
- Merging pull requests
- Releasing new versions

### Smoke Test Failures

If smoke tests fail, it indicates:
- **Health endpoint failures**: Application not starting correctly
- **Authentication failures**: JWT configuration issues
- **API failures**: Major routing or middleware problems

Smoke test failures should block deployments and require immediate attention.

## Integration with CI/CD

### GitHub Actions

```yaml
jobs:
  smoke-tests:
    name: Smoke Tests
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Run smoke tests
        run: dotnet test --filter "Category=Smoke" --logger "trx"
        
      - name: Upload results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: smoke-test-results
          path: '**/TestResults/*.trx'
```

### Local Pre-commit

```powershell
# Add to pre-commit hook
dotnet test --filter "Category=Smoke" --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Error "Smoke tests failed. Fix issues before committing."
    exit 1
}
```

## Test Data

Smoke tests use:
- No persistent test data
- In-memory implementations
- Mock/fake data generated during tests
- No cleanup required

## Troubleshooting

### Tests Fail Locally but Pass in CI

1. Check environment differences
2. Verify local dependencies (ports, services)
3. Ensure clean build: `dotnet clean && dotnet build`

### Authentication Tests Failing

1. Verify JWT configuration in `appsettings.json`
2. Check `JwtBearer` middleware registration
3. Ensure correct token format in tests

### Performance Tests Timing Out

1. Check system resources
2. Verify no background processes interfering
3. Increase timeout thresholds if needed

### Health Check Failures

1. Verify all dependencies are registered
2. Check health check implementations
3. Ensure readiness checks don't fail prematurely

## Best Practices

### Writing Smoke Tests

1. **Keep it simple**: Test one thing per test
2. **Be fast**: Target < 1 second per test
3. **Be reliable**: No flaky tests allowed
4. **Be informative**: Clear assertion messages
5. **Test critical paths**: Focus on must-work scenarios

### Maintaining Smoke Tests

1. **Run before committing**: Part of development workflow
2. **Update with API changes**: Keep in sync
3. **Monitor in CI**: Track execution times
4. **Fix immediately**: Don't ignore failures

## Metrics

Track smoke test metrics:
- **Execution time**: Should remain < 30 seconds
- **Pass rate**: Should be 100%
- **Flakiness**: Should be 0%
- **Coverage**: Critical endpoints covered

## References

- [ASP.NET Core Testing Best Practices](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [xUnit Documentation](https://xunit.net/)
- [Fluent Assertions](https://fluentassertions.com/)
- [WebApplicationFactory](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests#basic-tests-with-the-default-webapplicationfactory)
