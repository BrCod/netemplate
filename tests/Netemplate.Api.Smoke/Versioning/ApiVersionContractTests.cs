using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Netemplate.Api.Smoke.Versioning;

/// <summary>
/// Smoke tests validating API versioning contracts for concurrent v1 and v2 operation.
/// These tests verify that both API versions coexist, handle different request patterns,
/// and maintain data consistency while using different response formats.
/// 
/// Note: These are smoke tests that assume the API is already running (e.g., via Docker).
/// Run the API first with: dotnet run --project src/Api/Netemplate.Api.csproj
/// </summary>
public class ApiVersionContractTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiVersionContractTests()
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    [Fact]
    public async Task V1_And_V2_Endpoints_CoexistSuccessfully()
    {
        // Arrange: Create a product via V1
        var createRequest = new { Name = "Test Product", Price = 29.99, StockQuantity = 100 };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", createRequest);
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductDto>(_jsonOptions);
        var productId = createdProduct!.Id;

        try
        {
            // Act: Access same product via both versions
            var v1Response = await _client.GetAsync($"/api/v1/products/{productId}");
            var v2Response = await _client.GetAsync($"/api/v2/products/{productId}");

            // Assert: Both versions work
            v1Response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            v2Response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            // V1 returns direct DTO
            var v1Product = await v1Response.Content.ReadFromJsonAsync<ProductDto>(_jsonOptions);
            v1Product.Should().NotBeNull();
            v1Product!.Name.Should().Be("Test Product");

            // V2 returns wrapped response
            var v2Product = await v2Response.Content.ReadFromJsonAsync<ProductV2Response>(_jsonOptions);
            v2Product.Should().NotBeNull();
            v2Product!.Data.Should().NotBeNull();
            v2Product.Data!.Name.Should().Be("Test Product");
        }
        finally
        {
            // Cleanup
            await _client.DeleteAsync($"/api/v1/products/{productId}");
        }
    }

    [Fact]
    public async Task V1_List_UsesOffsetPagination()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/products?skip=0&take=10");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var listResponse = await response.Content.ReadFromJsonAsync<ProductListResponse>(_jsonOptions);
        listResponse.Should().NotBeNull();
        listResponse!.Items.Should().NotBeNull();
        listResponse.Total.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task V2_List_UsesCursorBasedPagination()
    {
        // Act
        var response = await _client.GetAsync("/api/v2/products?pageSize=10");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var listResponse = await response.Content.ReadFromJsonAsync<ProductListV2Response>(_jsonOptions);
        listResponse.Should().NotBeNull();
        listResponse!.Data.Should().NotBeNull();
        listResponse.Pagination.Should().NotBeNull();
        listResponse.Pagination!.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task V2_SupportsOptionalMetadata()
    {
        // Arrange: Create a product
        var createRequest = new { Name = "Metadata Test", Price = 19.99, StockQuantity = 50 };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", createRequest);
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductDto>(_jsonOptions);
        var productId = createdProduct!.Id;

        try
        {
            // Act: Request with and without metadata
            var withMetadata = await _client.GetAsync($"/api/v2/products/{productId}?includeMetadata=true");
            var withoutMetadata = await _client.GetAsync($"/api/v2/products/{productId}?includeMetadata=false");

            // Assert
            withMetadata.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            withoutMetadata.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var withMeta = await withMetadata.Content.ReadFromJsonAsync<ProductV2Response>(_jsonOptions);
            var withoutMeta = await withoutMetadata.Content.ReadFromJsonAsync<ProductV2Response>(_jsonOptions);

            withMeta!.Metadata.Should().NotBeNull();
            withMeta.Metadata!.CreatedAt.Should().NotBe(default);

            withoutMeta!.Metadata.Should().BeNull();
        }
        finally
        {
            // Cleanup
            await _client.DeleteAsync($"/api/v1/products/{productId}");
        }
    }

    [Fact]
    public async Task V1_Create_ReturnsProductDto()
    {
        // Arrange
        var createRequest = new { Name = "V1 Product", Price = 39.99, StockQuantity = 75 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/products", createRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var product = await response.Content.ReadFromJsonAsync<ProductDto>(_jsonOptions);
        product.Should().NotBeNull();
        product!.Name.Should().Be("V1 Product");
        product.Id.Should().NotBe(Guid.Empty);

        // Cleanup
        await _client.DeleteAsync($"/api/v1/products/{product.Id}");
    }

    [Fact]
    public async Task V2_Create_ReturnsWrappedResponse()
    {
        // Arrange
        var createRequest = new { Name = "V2 Product", Price = 49.99, StockQuantity = 60 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v2/products", createRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var wrappedResponse = await response.Content.ReadFromJsonAsync<ProductV2Response>(_jsonOptions);
        wrappedResponse.Should().NotBeNull();
        wrappedResponse!.Data.Should().NotBeNull();
        wrappedResponse.Data!.Name.Should().Be("V2 Product");

        // Cleanup
        await _client.DeleteAsync($"/api/v1/products/{wrappedResponse.Data.Id}");
    }

    [Fact]
    public async Task BothVersions_ReturnSameData_DifferentFormat()
    {
        // Arrange: Create a product
        var createRequest = new { Name = "Format Test", Price = 24.99, StockQuantity = 40 };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", createRequest);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductDto>(_jsonOptions);
        var productId = createdProduct!.Id;

        try
        {
            // Act: Get via both versions
            var v1Response = await _client.GetAsync($"/api/v1/products/{productId}");
            var v2Response = await _client.GetAsync($"/api/v2/products/{productId}");

            var v1Product = await v1Response.Content.ReadFromJsonAsync<ProductDto>(_jsonOptions);
            var v2Product = await v2Response.Content.ReadFromJsonAsync<ProductV2Response>(_jsonOptions);

            // Assert: Same data, different format
            v1Product!.Id.Should().Be(v2Product!.Data!.Id);
            v1Product.Name.Should().Be(v2Product.Data.Name);
            v1Product.Price.Should().Be(v2Product.Data.Price);
            v1Product.StockQuantity.Should().Be(v2Product.Data.StockQuantity);
        }
        finally
        {
            // Cleanup
            await _client.DeleteAsync($"/api/v1/products/{productId}");
        }
    }

    #region DTOs

    private record ProductDto(
        Guid Id,
        string Name,
        decimal Price,
        int StockQuantity
    );

    private record ProductListResponse(
        List<ProductDto> Items,
        int Total
    );

    private record ProductV2Response(
        ProductDto? Data,
        ProductMetadata? Metadata
    );

    private record ProductListV2Response(
        List<ProductDto>? Data,
        PaginationMetadata? Pagination,
        object? Metadata
    );

    private record PaginationMetadata(
        int PageSize,
        string? NextCursor,
        bool HasMore
    );

    private record ProductMetadata(
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

    #endregion
}
