using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

namespace Netemplate.Api.IntegrationTests;

public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthCheck_Live_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthCheck_Ready_ReturnsDetailedReport()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("status", content);
        Assert.Contains("checks", content);
        
        var report = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.True(report.TryGetProperty("status", out _));
        Assert.True(report.TryGetProperty("checks", out var checks));
        Assert.True(checks.GetArrayLength() > 0);
    }

    [Fact]
    public async Task HealthCheck_Ready_IncludesAllDependencies()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");
        var content = await response.Content.ReadAsStringAsync();
        var report = JsonSerializer.Deserialize<JsonElement>(content);

        // Assert
        var checks = report.GetProperty("checks");
        var checkNames = checks.EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .ToList();

        Assert.Contains("app_readiness", checkNames);
        Assert.Contains("postgres", checkNames);
        Assert.Contains("redis", checkNames);
        Assert.Contains("rabbitmq", checkNames);
    }
}
