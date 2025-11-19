using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netemplate.Application.DTOs;
using Netemplate.Application.Services;

namespace Netemplate.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
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
    /// Get product by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        _logger.LogInformation("Fetching product {ProductId}", id);
        
        var result = await _productService.GetByIdAsync(id, ct);
        
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Product {ProductId} not found", id);
            return NotFound(new ProblemDetails
            {
                Title = "Product not found",
                Detail = result.Error,
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// List products with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType<ProductListResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        if (take > 100) take = 100; // Max page size
        
        _logger.LogInformation("Listing products skip={Skip} take={Take}", skip, take);
        
        var result = await _productService.ListAsync(skip, take, ct);
        
        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Creating product {ProductName}", request.Name);
        
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

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Updating product {ProductId}", id);
        
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

        return Ok(result.Value);
    }

    /// <summary>
    /// Delete (deactivate) a product
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        _logger.LogInformation("Deleting product {ProductId}", id);
        
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
