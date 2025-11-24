using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Netemplate.Application.DTOs;

namespace Netemplate.Api.IntegrationTests.Contract.Versioning;

/// <summary>
/// Contract tests for concurrent API versions (v1 and v2)
/// Validates that both versions can coexist and behave correctly
/// </summary>
public sealed class ApiVersioningTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ApiVersioningTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task V1_And_V2_Endpoints_CoexistSuccessfully()
    {
        // Arrange - Create a product
        var createRequest = new
        {
            Name = "Test Product Versioning",
            Price = 99.99m,
            Currency = "USD",
            Description = "Testing version coexistence"
        };

        var token = GenerateTestJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Create via v1
        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var v1Created = await createResponse.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        v1Created.Should().NotBeNull();
        var productId = v1Created!.Id;

        // Act - Fetch from both versions
        var v1Response = await _client.GetAsync($"/api/v1/products/{productId}");
        var v2Response = await _client.GetAsync($"/api/v2/products/{productId}");

        // Assert - Both versions return success
        v1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        v2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert - V1 returns ProductDto directly
        var v1Product = await v1Response.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        v1Product.Should().NotBeNull();
        v1Product!.Id.Should().Be(productId);
        v1Product.Name.Should().Be("Test Product Versioning");

        // Assert - V2 returns wrapped response with metadata
        var v2Content = await v2Response.Content.ReadAsStringAsync();
        var v2Wrapper = JsonSerializer.Deserialize<JsonElement>(v2Content, JsonOptions);
        v2Wrapper.GetProperty("data").GetProperty("id").GetGuid().Should().Be(productId);
        v2Wrapper.GetProperty("data").GetProperty("name").GetString().Should().Be("Test Product Versioning");
    }

    [Fact]
    public async Task V1_List_UsesOffsetPagination()
    {
        // Arrange
        var token = GenerateTestJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - V1 list with skip/take parameters
        var response = await _client.GetAsync("/api/v1/products?skip=0&take=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var listResponse = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);

        // V1 response structure: { items, total, skip, take }
        listResponse.TryGetProperty("items", out _).Should().BeTrue("V1 uses 'items' field");
        listResponse.TryGetProperty("total", out _).Should().BeTrue("V1 uses 'total' field");
        listResponse.TryGetProperty("skip", out _).Should().BeTrue("V1 uses 'skip' field");
        listResponse.TryGetProperty("take", out _).Should().BeTrue("V1 uses 'take' field");
    }

    [Fact]
    public async Task V2_List_UsesCursorBasedPagination()
    {
        // Arrange
        var token = GenerateTestJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - V2 list with cursor and pageSize
        var response = await _client.GetAsync("/api/v2/products?pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var listResponse = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);

        // V2 response structure: { data, pagination, metadata }
        listResponse.TryGetProperty("data", out _).Should().BeTrue("V2 uses 'data' field");
        listResponse.TryGetProperty("pagination", out var pagination).Should().BeTrue("V2 has 'pagination' field");
        
        pagination.TryGetProperty("pageSize", out _).Should().BeTrue();
        pagination.TryGetProperty("currentCursor", out _).Should().BeTrue();
        pagination.TryGetProperty("nextCursor", out _).Should().BeTrue();
        pagination.TryGetProperty("totalCount", out _).Should().BeTrue();
    }

    [Fact]
    public async Task V2_SupportsOptionalMetadata()
    {
        // Arrange - Create a product first
        var createRequest = new
        {
            Name = "Metadata Test Product",
            Price = 49.99m,
            Currency = "USD"
        };

        var token = GenerateTestJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/v2/products", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var productId = created.GetProperty("data").GetProperty("id").GetGuid();

        // Act - Fetch without metadata
        var responseWithoutMeta = await _client.GetAsync($"/api/v2/products/{productId}?includeMetadata=false");
        var contentWithoutMeta = await responseWithoutMeta.Content.ReadAsStringAsync();
        var jsonWithoutMeta = JsonSerializer.Deserialize<JsonElement>(contentWithoutMeta, JsonOptions);

        // Act - Fetch with metadata
        var responseWithMeta = await _client.GetAsync($"/api/v2/products/{productId}?includeMetadata=true");
        var contentWithMeta = await responseWithMeta.Content.ReadAsStringAsync();
        var jsonWithMeta = JsonSerializer.Deserialize<JsonElement>(contentWithMeta, JsonOptions);

        // Assert - Without metadata
        jsonWithoutMeta.TryGetProperty("data", out _).Should().BeTrue();
        jsonWithoutMeta.TryGetProperty("metadata", out var metaWithoutFlag).Should().BeTrue();
        metaWithoutFlag.ValueKind.Should().Be(JsonValueKind.Null, "metadata should be null when not requested");

        // Assert - With metadata
        jsonWithMeta.TryGetProperty("metadata", out var metaWithFlag).Should().BeTrue();
        metaWithFlag.ValueKind.Should().NotBe(JsonValueKind.Null, "metadata should be present when requested");
        metaWithFlag.GetProperty("version").GetString().Should().Be("v2");
        metaWithFlag.GetProperty("source").GetString().Should().Be("primary-db");
    }

    [Fact]
    public async Task ConcurrentRequests_ToBothVersions_HandleCorrectly()
    {
        // Arrange
        var token = GenerateTestJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a product
        var createRequest = new
        {
            Name = "Concurrent Test Product",
            Price = 79.99m,
            Currency = "EUR"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        var productId = created!.Id;

        // Act - Fire concurrent requests to both versions
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_client.GetAsync($"/api/v1/products/{productId}"));
            tasks.Add(_client.GetAsync($"/api/v2/products/{productId}"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - All requests succeed
        foreach (var r in responses)
        {
            r.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Assert - V1 and V2 responses have different structures
        var v1Responses = responses.Where((_, idx) => idx % 2 == 0).ToList();
        var v2Responses = responses.Where((_, idx) => idx % 2 == 1).ToList();

        foreach (var v1Response in v1Responses)
        {
            var content = await v1Response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
            json.TryGetProperty("id", out _).Should().BeTrue("V1 has direct id field");
        }

        foreach (var v2Response in v2Responses)
        {
            var content = await v2Response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
            json.TryGetProperty("data", out _).Should().BeTrue("V2 wraps in data field");
        }
    }

    [Fact]
    public async Task V1_Create_ReturnsProductDto()
    {
        // Arrange
        var createRequest = new
        {
            Name = "V1 Create Test",
            Price = 29.99m,
            Currency = "GBP"
        };

        var token = GenerateTestJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/products", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);

        // V1 returns ProductDto directly
        json.TryGetProperty("id", out _).Should().BeTrue();
        json.TryGetProperty("name", out _).Should().BeTrue();
        json.GetProperty("name").GetString().Should().Be("V1 Create Test");
    }

    [Fact]
    public async Task V2_Create_ReturnsWrappedResponse()
    {
        // Arrange
        var createRequest = new
        {
            Name = "V2 Create Test",
            Price = 39.99m,
            Currency = "CAD"
        };

        var token = GenerateTestJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v2/products", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);

        // V2 returns wrapped response with data and metadata
        json.TryGetProperty("data", out var data).Should().BeTrue();
        json.TryGetProperty("metadata", out var metadata).Should().BeTrue();
        
        data.GetProperty("name").GetString().Should().Be("V2 Create Test");
        metadata.GetProperty("version").GetString().Should().Be("v2");
    }

    [Fact]
    public async Task V2_CursorPagination_NavigatesCorrectly()
    {
        // Arrange - Create multiple products
        var token = GenerateTestJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        for (int i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/v1/products", new
            {
                Name = $"Pagination Test {i}",
                Price = 10.00m + i,
                Currency = "USD"
            });
        }

        // Act - First page
        var firstPageResponse = await _client.GetAsync("/api/v2/products?pageSize=2");
        var firstPageContent = await firstPageResponse.Content.ReadAsStringAsync();
        var firstPage = JsonSerializer.Deserialize<JsonElement>(firstPageContent, JsonOptions);

        // Assert - First page has next cursor
        firstPage.GetProperty("pagination").TryGetProperty("nextCursor", out var nextCursor).Should().BeTrue();
        var nextCursorValue = nextCursor.GetString();
        nextCursorValue.Should().NotBeNullOrEmpty();

        // Act - Second page using cursor
        var secondPageResponse = await _client.GetAsync($"/api/v2/products?pageSize=2&cursor={nextCursorValue}");
        var secondPageContent = await secondPageResponse.Content.ReadAsStringAsync();
        var secondPage = JsonSerializer.Deserialize<JsonElement>(secondPageContent, JsonOptions);

        // Assert - Second page data is different
        var firstPageData = firstPage.GetProperty("data");
        var secondPageData = secondPage.GetProperty("data");
        
        firstPageData.GetArrayLength().Should().BeGreaterThan(0);
        secondPageData.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task BothVersions_ReturnSameData_DifferentFormat()
    {
        // Arrange
        var token = GenerateTestJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new
        {
            Name = "Format Comparison Product",
            Price = 123.45m,
            Currency = "USD",
            Description = "Testing format consistency"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        var productId = created!.Id;

        // Act
        var v1Response = await _client.GetAsync($"/api/v1/products/{productId}");
        var v2Response = await _client.GetAsync($"/api/v2/products/{productId}");

        var v1Content = await v1Response.Content.ReadAsStringAsync();
        var v2Content = await v2Response.Content.ReadAsStringAsync();

        var v1Json = JsonSerializer.Deserialize<JsonElement>(v1Content, JsonOptions);
        var v2Json = JsonSerializer.Deserialize<JsonElement>(v2Content, JsonOptions);

        // Assert - Same product data, different structure
        var v1Id = v1Json.GetProperty("id").GetGuid();
        var v2Id = v2Json.GetProperty("data").GetProperty("id").GetGuid();
        v1Id.Should().Be(v2Id);

        var v1Name = v1Json.GetProperty("name").GetString();
        var v2Name = v2Json.GetProperty("data").GetProperty("name").GetString();
        v1Name.Should().Be(v2Name).And.Be("Format Comparison Product");

        var v1Price = v1Json.GetProperty("price").GetDecimal();
        var v2Price = v2Json.GetProperty("data").GetProperty("price").GetDecimal();
        v1Price.Should().Be(v2Price).And.Be(123.45m);
    }

    [Fact]
    public async Task V1_And_V2_Updates_WorkIndependently()
    {
        // Arrange
        var token = GenerateTestJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new
        {
            Name = "Update Test Product",
            Price = 50.00m,
            Currency = "USD"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        var productId = created!.Id;

        // Act - Update via V1
        var v1UpdateRequest = new { Name = "Updated via V1", Price = 55.00m };
        var v1UpdateResponse = await _client.PutAsJsonAsync($"/api/v1/products/{productId}", v1UpdateRequest);

        // Assert - V1 update returns ProductDto
        v1UpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var v1Updated = await v1UpdateResponse.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        v1Updated!.Name.Should().Be("Updated via V1");

        // Act - Update via V2
        var v2UpdateRequest = new { Name = "Updated via V2", Price = 60.00m };
        var v2UpdateResponse = await _client.PutAsJsonAsync($"/api/v2/products/{productId}", v2UpdateRequest);

        // Assert - V2 update returns wrapped response
        v2UpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var v2Content = await v2UpdateResponse.Content.ReadAsStringAsync();
        var v2Updated = JsonSerializer.Deserialize<JsonElement>(v2Content, JsonOptions);
        v2Updated.GetProperty("data").GetProperty("name").GetString().Should().Be("Updated via V2");
        v2Updated.GetProperty("metadata").GetProperty("version").GetString().Should().Be("v2");
    }

    private static string GenerateTestJwtToken()
    {
        // Return a simple test token that matches the JWT configuration in TestWebApplicationFactory
        return "test-token-for-integration-tests";
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
