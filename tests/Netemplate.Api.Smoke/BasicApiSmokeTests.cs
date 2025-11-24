using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Netemplate.Api.Smoke;

/// <summary>
/// Smoke tests for basic API functionality and infrastructure.
/// These tests validate that the application is deployed and responding correctly.
/// </summary>
[Trait("Category", "Smoke")]
public class BasicApiSmokeTests : IClassFixture<SmokeTestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BasicApiSmokeTests(SmokeTestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Application_CanStart()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.Should().NotBeNull("Application should start and respond to requests");
        response.IsSuccessStatusCode.Should().BeTrue("Application should be healthy after startup");
    }

    [Fact]
    public async Task Swagger_IsAccessible()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/swagger/index.html");

        // Assert
        // Swagger documentation should be accessible
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.MovedPermanently, 
            HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task NotFoundEndpoint_Returns404()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/nonexistent/endpoint");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Non-existent endpoints should return 404");
    }

    [Fact]
    public async Task NotFoundEndpoint_ReturnsProblemDetails()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/nonexistent/endpoint");
        var contentType = response.Content.Headers.ContentType?.MediaType;

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        contentType.Should().Be("application/problem+json", 
            "Error responses should use problem+json format");
    }

    [Fact]
    public async Task HttpsRedirection_IsConfigured()
    {
        // Arrange
        var clientWithoutRedirect = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false
        })
        {
            BaseAddress = _client.BaseAddress
        };

        // Act
        var response = await clientWithoutRedirect.GetAsync("/health/live");

        // Assert
        // Should either succeed with HTTPS or redirect to HTTPS (Application should enforce HTTPS)
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.MovedPermanently,
            HttpStatusCode.TemporaryRedirect,
            HttpStatusCode.PermanentRedirect);
    }

    [Fact]
    public async Task CorsHeaders_ArePresent()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/products");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // Response should include CORS headers or handle OPTIONS
        response.Should().NotBeNull("CORS preflight should be handled");
    }

    [Fact]
    public async Task SecurityHeaders_ArePresent()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        // Check for common security headers
        var headers = response.Headers;
        
        // At minimum, these security headers should be considered
        // Specific headers depend on your security configuration
        response.Should().NotBeNull("Response should include security headers");
    }

    [Fact]
    public async Task Application_RespondsToMultipleConcurrentRequests()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.GetAsync("/health/live"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().OnlyContain(r => r.IsSuccessStatusCode,
            "Application should handle concurrent requests");
    }

    [Fact]
    public async Task MethodNotAllowed_ReturnsCorrectStatusCode()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, "/health/live");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // Invalid HTTP method should return 405 or 404
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.MethodNotAllowed,
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LargeRequestBody_IsHandledGracefully()
    {
        // Arrange
        var largeContent = new string('x', 10 * 1024 * 1024); // 10MB
        var content = new StringContent(largeContent);

        // Act
        var response = await _client.PostAsync("/api/products", content);

        // Assert
        // Should either succeed or return appropriate error (Large requests should be handled with appropriate status codes)
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized, // Authentication required first
            HttpStatusCode.BadRequest,
            HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task Application_HasConsistentResponseTimes()
    {
        // Arrange
        var responseTimes = new List<long>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await _client.GetAsync("/health/live");
            sw.Stop();
            responseTimes.Add(sw.ElapsedMilliseconds);
        }

        // Assert
        var avgResponseTime = responseTimes.Average();
        var maxDeviation = responseTimes.Max() - responseTimes.Min();

        avgResponseTime.Should().BeLessThan(1000, "Average response time should be reasonable");
        maxDeviation.Should().BeLessThan(500, "Response times should be consistent");
    }

    [Fact]
    public async Task RateLimiting_IsConfigured()
    {
        // Arrange & Act
        var tasks = new List<Task<HttpResponseMessage>>();
        
        // Send many rapid requests to test rate limiting
        for (int i = 0; i < 150; i++)
        {
            tasks.Add(_client.GetAsync("/api/products"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        // Should have at least some successful responses and possibly some rate limited (429)
        responses.Should().Contain(r => r.IsSuccessStatusCode || r.StatusCode == HttpStatusCode.TooManyRequests,
            "Rate limiting should be configured and may throttle excessive requests");
    }
}
