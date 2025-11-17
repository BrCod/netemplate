using Microsoft.AspNetCore.Mvc;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Policies;
using Polly;

namespace Api.Controllers
{
    /// <summary>
    /// Controller for managing Product operations with resilience policies applied at the boundary.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IRepository<Product> _productRepository;
        private readonly Application.Services.ProductService _productService;
        private readonly ResiliencePolicyProvider _policyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductController"/> class.
        /// </summary>
        /// <param name="productRepository">The product repository.</param>
        /// <param name="productService">The product service.</param>
        /// <param name="policyProvider">The resilience policy provider.</param>
        public ProductController(
            IRepository<Product> productRepository,
            Application.Services.ProductService productService,
            ResiliencePolicyProvider policyProvider)
        {
            _productRepository = productRepository;
            _productService = productService;
            _policyProvider = policyProvider;
        }

        /// <summary>
        /// Gets all products or a paginated list.
        /// </summary>
        /// <param name="limit">The maximum number of items to return.</param>
        /// <param name="cursor">The cursor for pagination.</param>
        /// <returns>A list of products or paginated result.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? limit, [FromQuery] string? cursor)
        {
            if (limit == null && cursor == null)
            {
                var products = await _productRepository.GetAllAsync();
                return Ok(products);
            }
            var (items, nextCursor) = await _productService.ListPagedAsync(limit ?? 20, cursor);
            return Ok(new { items, nextCursor });
        }

        /// <summary>
        /// Gets a product by its ID.
        /// </summary>
        /// <param name="id">The product ID.</param>
        /// <returns>The product if found.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var repositoryPolicy = _policyProvider.GetRepositoryPolicy<Product?>();
            var product = await repositoryPolicy.ExecuteAsync(async () =>
                await _productRepository.GetByIdAsync(id)
            );
            if (product == null) return NotFound();
            return Ok(product);
        }

        /// <summary>
        /// Creates a new product.
        /// </summary>
        /// <param name="dto">The product data transfer object.</param>
        /// <returns>The created product.</returns>
        [HttpPost]
        public async Task<IActionResult> Create(ProductDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }
            
            var repositoryPolicy = _policyProvider.GetRepositoryPolicy<Product>();
            var product = await repositoryPolicy.ExecuteAsync(async () =>
                await _productService.AddAsync(dto)
            );
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        /// <summary>
        /// Updates an existing product.
        /// </summary>
        /// <param name="id">The product ID.</param>
        /// <param name="dto">The product data transfer object.</param>
        /// <returns>The updated product.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, ProductDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }
            
            var repositoryPolicy = _policyProvider.GetRepositoryPolicy<Product?>();
            var product = await repositoryPolicy.ExecuteAsync(async () =>
                await _productRepository.GetByIdAsync(id)
            );
            if (product == null) return NotFound();
            
            // Execute update through policy wrapper
            await repositoryPolicy.ExecuteAsync(async () =>
            {
                await _productService.UpdateAsync(id, dto);
                return (Product?)null; // Return type match for policy
            });
            
            // Fetch updated product with policy
            product = await repositoryPolicy.ExecuteAsync(async () =>
                await _productRepository.GetByIdAsync(id)
            );
            return Ok(product);
        }

        /// <summary>
        /// Deletes a product by its ID.
        /// </summary>
        /// <param name="id">The product ID.</param>
        /// <returns>No content if successful.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var repositoryPolicy = _policyProvider.GetRepositoryPolicy<bool>();
            var success = await repositoryPolicy.ExecuteAsync(async () =>
                await _productService.DeactivateAsync(id, "User requested deletion")
            );
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
