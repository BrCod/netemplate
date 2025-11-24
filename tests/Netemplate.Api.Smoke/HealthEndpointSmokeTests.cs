using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Netemplate.Api.Smoke;

/// <summary>
/// Smoke tests for health check endpoints.
/// These tests validate that the application can start and respond to basic health checks.
/// </summary>
[Trait("Category", "Smoke")]
public class HealthEndpointSmokeTests : IClassFixture<SmokeTestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointSmokeTests(SmokeTestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthLive_ReturnsOk()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthLive_ReturnsHealthyStatus()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health/live");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task HealthLive_ResponseTimeIsAcceptable()
    {
        // Arrange
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/health/live");
        sw.Stop();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        sw.ElapsedMilliseconds.Should().BeLessThan(1000, "Liveness check should respond within 1 second");
    }

    [Fact]
    public async Task HealthReady_ReturnsSuccessStatusCode()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue("Readiness check should return success when app is ready");
    }

    [Fact]
    public async Task HealthReady_ResponseTimeIsAcceptable()
    {
        // Arrange
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/health/ready");
        sw.Stop();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        sw.ElapsedMilliseconds.Should().BeLessThan(5000, "Readiness check should respond within 5 seconds");
    }

    [Fact]
    public async Task HealthReady_ReturnsJsonContentType()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task HealthReady_CanBeCalledMultipleTimes()
    {
        // Arrange & Act
        var response1 = await _client.GetAsync("/health/ready");
        var response2 = await _client.GetAsync("/health/ready");
        var response3 = await _client.GetAsync("/health/ready");

        // Assert
        response1.IsSuccessStatusCode.Should().BeTrue();
        response2.IsSuccessStatusCode.Should().BeTrue();
        response3.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Health_RootEndpoint_ReturnsOk()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_InvalidEndpoint_ReturnsNotFound()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HealthChecks_DontRequireAuthentication()
    {
        // Arrange
        var client = new HttpClient
        {
            BaseAddress = _client.BaseAddress
        };
        // Explicitly not setting Authorization header

        // Act
        var liveResponse = await client.GetAsync("/health/live");
        var readyResponse = await client.GetAsync("/health/ready");

        // Assert
        liveResponse.IsSuccessStatusCode.Should().BeTrue("Liveness check should not require authentication");
        readyResponse.IsSuccessStatusCode.Should().BeTrue("Readiness check should not require authentication");
    }

    [Fact]
    public async Task HealthLive_ConsecutiveCalls_AreConsistent()
    {
        // Arrange & Act
        var results = new List<HttpStatusCode>();
        for (int i = 0; i < 5; i++)
        {
            var response = await _client.GetAsync("/health/live");
            results.Add(response.StatusCode);
        }

        // Assert
        results.Should().OnlyContain(status => status == HttpStatusCode.OK, 
            "Consecutive health checks should return consistent results");
    }
}
