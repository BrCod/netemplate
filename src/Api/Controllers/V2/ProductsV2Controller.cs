using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netemplate.Application.DTOs;
using Netemplate.Application.Services;

namespace Netemplate.Api.Controllers.V2;

/// <summary>
/// Products API v2 - Breaking changes:
/// - Renamed endpoints (GetProduct, ListProducts)
/// - Added include parameter for related data
/// - Changed response format (added metadata)
/// - Enhanced pagination with cursor support
/// </summary>
[ApiController]
[Route("api/v2/[controller]")]
[Authorize]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Get a single product by ID (V2 - enhanced with metadata)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductV2Response>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(Guid id, [FromQuery] bool includeMetadata = false, CancellationToken ct = default)
    {
        _logger.LogInformation("V2: Fetching product {ProductId} with metadata={IncludeMetadata}", id, includeMetadata);
        
        var result = await _productService.GetByIdAsync(id, ct);
        
        if (!result.IsSuccess)
        {
            _logger.LogWarning("V2: Product {ProductId} not found", id);
            return NotFound(new ProblemDetails
            {
                Title = "Product not found",
                Detail = result.Error,
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        var response = new ProductV2Response
        {
            Data = result.Value!,
            Metadata = includeMetadata ? new ProductMetadata
            {
                Version = "v2",
                RetrievedAt = DateTime.UtcNow,
                Source = "primary-db"
            } : null
        };

        return Ok(response);
    }

    /// <summary>
    /// List products with cursor-based pagination (V2)
    /// </summary>
    [HttpGet]
    [ProducesResponseType<ProductListV2Response>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListProducts(
        [FromQuery] int pageSize = 20, 
        [FromQuery] string? cursor = null,
        [FromQuery] bool includeMetadata = false,
        CancellationToken ct = default)
    {
        if (pageSize > 100) pageSize = 100;
        if (pageSize < 1) pageSize = 20;
        
        // Parse cursor (in real implementation, cursor would be encoded)
        var skip = 0;
        if (!string.IsNullOrEmpty(cursor) && int.TryParse(cursor, out var cursorValue))
        {
            skip = cursorValue;
        }
        
        _logger.LogInformation("V2: Listing products pageSize={PageSize} cursor={Cursor}", pageSize, cursor);
        
        var result = await _productService.ListAsync(skip, pageSize, ct);
        
        var response = new ProductListV2Response
        {
            Data = result.Value!.Items,
            Pagination = new PaginationMetadata
            {
                PageSize = pageSize,
                CurrentCursor = cursor,
                NextCursor = (skip + pageSize < result.Value.Total) ? (skip + pageSize).ToString() : null,
                TotalCount = result.Value.Total
            },
            Metadata = includeMetadata ? new ProductMetadata
            {
                Version = "v2",
                RetrievedAt = DateTime.UtcNow,
                Source = "primary-db"
            } : null
        };

        return Ok(response);
    }

    /// <summary>
    /// Create a new product (V2 - same as v1 but returns v2 response format)
    /// </summary>
    [HttpPost]
    [ProducesResponseType<ProductV2Response>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        _logger.LogInformation("V2: Creating product {ProductName}", request.Name);
        
        var result = await _productService.CreateAsync(request, ct);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var response = new ProductV2Response
        {
            Data = result.Value!,
            Metadata = new ProductMetadata
            {
                Version = "v2",
                RetrievedAt = DateTime.UtcNow,
                Source = "primary-db"
            }
        };

        return CreatedAtAction(nameof(GetProduct), new { id = result.Value!.Id }, response);
    }

    /// <summary>
    /// Update a product (V2 - same logic, different response format)
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProductV2Response>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        _logger.LogInformation("V2: Updating product {ProductId}", id);
        
        var result = await _productService.UpdateAsync(id, request, ct);
        
        if (!result.IsSuccess)
        {
            var statusCode = result.Error == "Product not found" 
                ? StatusCodes.Status404NotFound 
                : StatusCodes.Status400BadRequest;
                
            return StatusCode(statusCode, new ProblemDetails
            {
                Title = result.Error == "Product not found" ? "Not found" : "Validation failed",
                Detail = result.Error,
                Status = statusCode,
                Instance = HttpContext.Request.Path
            });
        }

        var response = new ProductV2Response
        {
            Data = result.Value!,
            Metadata = new ProductMetadata
            {
                Version = "v2",
                RetrievedAt = DateTime.UtcNow,
                Source = "primary-db"
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Delete a product (V2 - same as v1)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken ct)
    {
        _logger.LogInformation("V2: Deleting product {ProductId}", id);
        
        var result = await _productService.DeleteAsync(id, ct);
        
        if (!result.IsSuccess)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Product not found",
                Detail = result.Error,
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        return NoContent();
    }
}

/// <summary>
/// V2 response wrapper with optional metadata
/// </summary>
public sealed record ProductV2Response
{
    public required ProductDto Data { get; init; }
    public ProductMetadata? Metadata { get; init; }
}

/// <summary>
/// V2 list response with cursor-based pagination
/// </summary>
public sealed record ProductListV2Response
{
    public required IEnumerable<ProductDto> Data { get; init; }
    public required PaginationMetadata Pagination { get; init; }
    public ProductMetadata? Metadata { get; init; }
}

public sealed record PaginationMetadata
{
    public required int PageSize { get; init; }
    public required string? CurrentCursor { get; init; }
    public required string? NextCursor { get; init; }
    public required int TotalCount { get; init; }
}

public sealed record ProductMetadata
{
    public required string Version { get; init; }
    public required DateTime RetrievedAt { get; init; }
    public required string Source { get; init; }
}
