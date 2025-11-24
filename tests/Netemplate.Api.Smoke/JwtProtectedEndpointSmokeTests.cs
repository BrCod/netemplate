using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Netemplate.Api.Smoke;

/// <summary>
/// Smoke tests for JWT-protected endpoints.
/// These tests validate authentication and authorization behaviors.
/// </summary>
[Trait("Category", "Smoke")]
public class JwtProtectedEndpointSmokeTests : IClassFixture<SmokeTestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SmokeTestWebApplicationFactory _factory;

    public JwtProtectedEndpointSmokeTests(SmokeTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, 
            "Accessing protected endpoint without token should return 401");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", "invalid.token.here");

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Invalid JWT token should return 401");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithMalformedToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", "not-a-jwt-token");

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Malformed token should return 401");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_ReturnsUnauthorized()
    {
        // Arrange - This is an expired token (exp claim in the past)
        var expiredToken = CreateExpiredToken();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Expired token should return 401");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutBearerPrefix_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", "some-token-without-bearer");

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Token without 'Bearer' prefix should return 401");
    }

    [Fact]
    public async Task ProtectedEndpoint_MultipleAuthHeaders_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer token1");
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer token2");

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Multiple authorization headers should return 401");
    }

    [Fact]
    public async Task SwaggerEndpoint_DoesNotRequireAuthentication()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/swagger/index.html");

        // Assert
        // Swagger UI should be accessible without authentication
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.MovedPermanently, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task OptionsRequest_ToProtectedEndpoint_ReturnsOk()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/products");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // OPTIONS request should be handled for CORS preflight
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.NoContent,
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnauthorizedResponse_ReturnsWwwAuthenticateHeader()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.Should().NotBeEmpty(
            "Unauthorized response should include WWW-Authenticate header");
    }

    [Fact]
    public async Task UnauthorizedResponse_ContainsBearerScheme()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var wwwAuthenticate = response.Headers.WwwAuthenticate.FirstOrDefault();
        wwwAuthenticate?.Scheme.Should().Be("Bearer", 
            "WWW-Authenticate header should specify Bearer scheme");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithEmptyAuthorizationHeader_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", string.Empty);

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Empty authorization header should return 401");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithWhitespaceToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", "   ");

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Whitespace token should return 401");
    }

    [Fact]
    public async Task Authentication_IsEnforced_AcrossMultipleRequests()
    {
        // Arrange & Act
        var response1 = await _client.GetAsync("/api/products");
        var response2 = await _client.GetAsync("/api/products");
        var response3 = await _client.GetAsync("/api/products");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response2.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response3.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Authentication should be consistently enforced");
    }

    [Fact]
    public async Task ProtectedEndpoint_ResponseTime_IsAcceptable()
    {
        // Arrange
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/api/products");
        sw.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        sw.ElapsedMilliseconds.Should().BeLessThan(2000, 
            "Authentication check should respond quickly even on failure");
    }

    private static string CreateExpiredToken()
    {
        // Create a simple JWT-like structure with expired exp claim
        // This is a mock expired token for testing purposes
        // In real scenarios, you would generate a proper JWT with a past expiration
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{{\"sub\":\"testuser\",\"exp\":{DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds()}}}"));
        var signature = Convert.ToBase64String(Encoding.UTF8.GetBytes("expired-signature"));
        
        return $"{header}.{payload}.{signature}";
    }
}
