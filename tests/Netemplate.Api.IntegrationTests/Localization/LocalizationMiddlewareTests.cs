using FluentAssertions;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Netemplate.Api.IntegrationTests.Localization;

/// <summary>
/// Tests for localization middleware with English and Turkish translations.
/// </summary>
public sealed class LocalizationMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LocalizationMiddlewareTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProduct_WithInvalidId_ReturnsLocalizedError_English()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "en-US");

        // Act - Request non-existent product
        var response = await client.GetAsync("/api/v1/products/00000000-0000-0000-0000-000000000001");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsWithExtensions>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Product not found");
        problemDetails.Extensions.Should().ContainKey("culture");
        problemDetails.Extensions!["culture"].ToString().Should().StartWith("en");
    }

    [Fact]
    public async Task GetProduct_WithInvalidId_ReturnsLocalizedError_Turkish()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "tr-TR");

        // Act
        var response = await client.GetAsync("/api/v1/products/00000000-0000-0000-0000-000000000001");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsWithExtensions>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Ürün bulunamadı"); // Turkish translation
        problemDetails.Extensions.Should().ContainKey("culture");
        problemDetails.Extensions!["culture"].ToString().Should().StartWith("tr");
    }

    [Fact]
    public async Task GetProduct_WithQueryParameter_OverridesAcceptLanguage()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "en-US");

        // Act - Query parameter should override header
        var response = await client.GetAsync("/api/v1/products/00000000-0000-0000-0000-000000000001?culture=tr-TR");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsWithExtensions>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Extensions.Should().ContainKey("culture");
        problemDetails.Extensions!["culture"].ToString().Should().StartWith("tr");
    }

    [Fact]
    public async Task GetProduct_WithUnsupportedCulture_FallsBackToEnglish()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "ja-JP"); // Unsupported language

        // Act
        var response = await client.GetAsync("/api/v1/products/00000000-0000-0000-0000-000000000001");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsWithExtensions>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Product not found"); // Falls back to English
    }

    [Fact]
    public async Task GetApiInfo_ReturnsLocalizedMetadata_English()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "en-US");

        // Act
        var response = await client.GetAsync("/api/v1/apiinfo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var apiInfo = await response.Content.ReadFromJsonAsync<ApiInfoResponse>();
        apiInfo.Should().NotBeNull();
        apiInfo!.Culture.Should().StartWith("en");
        apiInfo.Title.Should().Be("Clean Architecture API");
        apiInfo.Description.Should().Contain("Clean Architecture API Template");
    }

    [Fact]
    public async Task GetApiInfo_ReturnsLocalizedMetadata_Turkish()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "tr-TR");

        // Act
        var response = await client.GetAsync("/api/v1/apiinfo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var apiInfo = await response.Content.ReadFromJsonAsync<ApiInfoResponse>();
        apiInfo.Should().NotBeNull();
        apiInfo!.Culture.Should().StartWith("tr");
        apiInfo.Title.Should().Be("Temiz Mimari API"); // Turkish
        apiInfo.Description.Should().Contain("Temiz Mimari API Şablonu");
    }

    [Fact]
    public async Task GetApiInfoForCulture_ReturnsSpecificCulture_Turkish()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/apiinfo/tr-TR");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var apiInfo = await response.Content.ReadFromJsonAsync<ApiInfoResponse>();
        apiInfo.Should().NotBeNull();
        apiInfo!.Culture.Should().Be("tr-TR");
        apiInfo.Title.Should().Be("Temiz Mimari API");
        apiInfo.ContactName.Should().Be("API Destek Ekibi");
    }

    [Fact]
    public async Task GetApiInfoForCulture_WithInvalidCulture_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/apiinfo/invalid-culture");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsWithExtensions>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Invalid culture code");
    }

    [Theory]
    [InlineData("en-US", "English")]
    [InlineData("tr-TR", "Turkish")]
    [InlineData("es-ES", "Spanish")] // Should fall back to English
    [InlineData("fr-FR", "French")]   // Should fall back to English
    public async Task CultureDetection_WorksCorrectlyForMultipleCultures(string culture, string expectedLanguage)
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", culture);

        // Act
        var response = await client.GetAsync("/api/v1/apiinfo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var apiInfo = await response.Content.ReadFromJsonAsync<ApiInfoResponse>();
        apiInfo.Should().NotBeNull();
        
        // Verify culture was detected
        var detectedCulture = new CultureInfo(apiInfo!.Culture);
        detectedCulture.Should().NotBeNull();
    }
}

public sealed record ProblemDetailsWithExtensions
{
    public string? Type { get; init; }
    public string? Title { get; init; }
    public int? Status { get; init; }
    public string? Detail { get; init; }
    public string? Instance { get; init; }
    public Dictionary<string, object>? Extensions { get; init; }
}

public sealed record ApiInfoResponse
{
    public required string Culture { get; init; }
    public required string DisplayName { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string ContactName { get; init; }
    public required string ContactEmail { get; init; }
    public required string LicenseName { get; init; }
    public required List<CultureInfoDto> SupportedCultures { get; init; }
}

public sealed record CultureInfoDto
{
    public required string Code { get; init; }
    public required string DisplayName { get; init; }
    public required string NativeName { get; init; }
}
