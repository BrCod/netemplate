using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Globalization;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Localization;
using Netemplate.Api.Middleware.Localization;

namespace Netemplate.Api.Tests.Middleware;

public class LocalizationMiddlewareTests
{
    private readonly Mock<IStringLocalizer<LocalizationMiddleware>> _localizerMock;
    private readonly ILogger<LocalizationMiddleware> _logger;
    private readonly DefaultHttpContext _httpContext;

    public LocalizationMiddlewareTests()
    {
        _localizerMock = new Mock<IStringLocalizer<LocalizationMiddleware>>();
        _logger = NullLogger<LocalizationMiddleware>.Instance;
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
    }

    [Fact]
    public async Task InvokeAsync_NonProblemJsonResponse_PassesThrough()
    {
        // Arrange
        var middleware = new LocalizationMiddleware(
            next: async (innerContext) =>
            {
                innerContext.Response.ContentType = "application/json";
                await innerContext.Response.WriteAsync("{\"message\":\"success\"}");
            },
            localizer: _localizerMock.Object,
            logger: _logger);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        responseText.Should().Be("{\"message\":\"success\"}");
    }

    [Fact]
    public async Task InvokeAsync_ProblemJsonResponse_LocalizesTitle()
    {
        // Arrange
        var originalTitle = "Invalid request";
        var localizedTitle = "Solicitud inválida";

        _localizerMock
            .Setup(l => l[originalTitle])
            .Returns(new LocalizedString(originalTitle, localizedTitle, false));

        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = originalTitle,
            status = 400
        };

        var middleware = new LocalizationMiddleware(
            next: async (innerContext) =>
            {
                innerContext.Response.ContentType = "application/problem+json";
                await innerContext.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
            },
            localizer: _localizerMock.Object,
            logger: _logger);

        _httpContext.Features.Set<IRequestCultureFeature>(new RequestCultureFeature(
            new RequestCulture(new CultureInfo("es-ES")),
            null));

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseText);
        
        response.Should().ContainKey("title");
        response!["title"].GetString().Should().Be(localizedTitle);
        
        response.Should().ContainKey("extensions");
        var extensions = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(response["extensions"].GetRawText());
        extensions!["culture"].GetString().Should().Be("es-ES");
    }

    [Fact]
    public async Task InvokeAsync_ProblemJsonResponse_LocalizesDetail()
    {
        // Arrange
        var originalDetail = "Product not found";
        var localizedDetail = "Producto no encontrado";

        _localizerMock
            .Setup(l => l[originalDetail])
            .Returns(new LocalizedString(originalDetail, localizedDetail, false));

        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            title = "Not Found",
            status = 404,
            detail = originalDetail
        };

        var middleware = new LocalizationMiddleware(
            next: async (innerContext) =>
            {
                innerContext.Response.ContentType = "application/problem+json";
                await innerContext.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
            },
            localizer: _localizerMock.Object,
            logger: _logger);

        _httpContext.Features.Set<IRequestCultureFeature>(new RequestCultureFeature(
            new RequestCulture(new CultureInfo("es-ES")),
            null));

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseText);
        
        response.Should().ContainKey("detail");
        response!["detail"].GetString().Should().Be(localizedDetail);
    }

    [Fact]
    public async Task InvokeAsync_NoResourceFound_ReturnsOriginalMessage()
    {
        // Arrange
        var originalTitle = "Some unknown error";

        _localizerMock
            .Setup(l => l[originalTitle])
            .Returns(new LocalizedString(originalTitle, originalTitle, true)); // resourceNotFound = true

        var problemDetails = new
        {
            title = originalTitle,
            status = 500
        };

        var middleware = new LocalizationMiddleware(
            next: async (innerContext) =>
            {
                innerContext.Response.ContentType = "application/problem+json";
                await innerContext.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
            },
            localizer: _localizerMock.Object,
            logger: _logger);

        _httpContext.Features.Set<IRequestCultureFeature>(new RequestCultureFeature(
            new RequestCulture(new CultureInfo("fr-FR")),
            null));

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseText);
        
        response.Should().ContainKey("title");
        response!["title"].GetString().Should().Be(originalTitle); // Original message returned
    }

    [Fact]
    public async Task InvokeAsync_DefaultCulture_UsesEnglish()
    {
        // Arrange
        var originalTitle = "An error occurred while processing your request.";

        _localizerMock
            .Setup(l => l[originalTitle])
            .Returns(new LocalizedString(originalTitle, originalTitle, false));

        var problemDetails = new
        {
            title = originalTitle,
            status = 500
        };

        var middleware = new LocalizationMiddleware(
            next: async (innerContext) =>
            {
                innerContext.Response.ContentType = "application/problem+json";
                await innerContext.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
            },
            localizer: _localizerMock.Object,
            logger: _logger);

        // No RequestCultureFeature set - should default to CurrentCulture

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseText);
        
        response.Should().ContainKey("title");
        response!["title"].GetString().Should().Be(originalTitle);
    }

    [Fact]
    public async Task InvokeAsync_PreservesExistingExtensions()
    {
        // Arrange
        var originalTitle = "Invalid request";
        var localizedTitle = "Demande invalide";

        _localizerMock
            .Setup(l => l[originalTitle])
            .Returns(new LocalizedString(originalTitle, localizedTitle, false));

        var problemDetails = new
        {
            title = originalTitle,
            status = 400,
            extensions = new
            {
                traceId = "test-trace-id",
                correlationId = "test-correlation-id"
            }
        };

        var middleware = new LocalizationMiddleware(
            next: async (innerContext) =>
            {
                innerContext.Response.ContentType = "application/problem+json";
                await innerContext.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
            },
            localizer: _localizerMock.Object,
            logger: _logger);

        _httpContext.Features.Set<IRequestCultureFeature>(new RequestCultureFeature(
            new RequestCulture(new CultureInfo("fr-FR")),
            null));

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseText);
        
        response.Should().ContainKey("extensions");
        var extensions = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(response!["extensions"].GetRawText());
        
        extensions.Should().ContainKey("traceId");
        extensions.Should().ContainKey("correlationId");
        extensions.Should().ContainKey("culture");
        extensions!["culture"].GetString().Should().Be("fr-FR");
    }

    [Fact]
    public async Task InvokeAsync_InvalidJson_ReturnsOriginalResponse()
    {
        // Arrange
        var invalidJson = "{ this is not valid json }";

        var middleware = new LocalizationMiddleware(
            next: async (innerContext) =>
            {
                innerContext.Response.ContentType = "application/problem+json";
                await innerContext.Response.WriteAsync(invalidJson);
            },
            localizer: _localizerMock.Object,
            logger: _logger);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        responseText.Should().Be(invalidJson); // Original response returned
    }

    [Fact]
    public async Task InvokeAsync_EmptyResponse_DoesNothing()
    {
        // Arrange
        var middleware = new LocalizationMiddleware(
            next: async (innerContext) =>
            {
                innerContext.Response.ContentType = "application/problem+json";
                // Write empty response
            },
            localizer: _localizerMock.Object,
            logger: _logger);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        responseText.Should().BeEmpty();
    }
}
