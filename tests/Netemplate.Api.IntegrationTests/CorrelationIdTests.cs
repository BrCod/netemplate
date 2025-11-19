using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Netemplate.Api.IntegrationTests;

public class CorrelationIdTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Request_WithoutCorrelationId_GeneratesNewId()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        Assert.True(response.Headers.Contains(CorrelationIdHeader));
        var correlationId = response.Headers.GetValues(CorrelationIdHeader).First();
        Assert.False(string.IsNullOrEmpty(correlationId));
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task Request_WithCorrelationId_PreservesId()
    {
        // Arrange
        var client = _factory.CreateClient();
        var expectedCorrelationId = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add(CorrelationIdHeader, expectedCorrelationId);

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        Assert.True(response.Headers.Contains(CorrelationIdHeader));
        var actualCorrelationId = response.Headers.GetValues(CorrelationIdHeader).First();
        Assert.Equal(expectedCorrelationId, actualCorrelationId);
    }

    [Fact]
    public async Task MultipleRequests_DifferentCorrelationIds()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response1 = await client.GetAsync("/health/live");
        var response2 = await client.GetAsync("/health/live");

        // Assert
        var correlationId1 = response1.Headers.GetValues(CorrelationIdHeader).First();
        var correlationId2 = response2.Headers.GetValues(CorrelationIdHeader).First();
        Assert.NotEqual(correlationId1, correlationId2);
    }
}
