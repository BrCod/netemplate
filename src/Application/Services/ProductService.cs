using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Application.Services
{
    /// <summary>
    /// Service for managing Product operations with cache-aside caching and domain events.
    /// </summary>
    public class ProductService
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IOutboxRepository _outboxRepository;
        private readonly ICache _cache;

        // Cache keys and constants
        private const string ProductKeyPrefix = "product";
        private const string ProductListKey = "products:list";
        private static string GetProductKey(Guid id) => $"{ProductKeyPrefix}:{id}";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

        // OpenTelemetry observability
        private static readonly ActivitySource ActivitySource = new("NetemplateApi.ProductService");
        private static readonly Meter Meter = new("NetemplateApi.ProductService");
        private static readonly Counter<long> ProductsCreatedCounter = Meter.CreateCounter<long>("products_created_total", description: "Total products created");
        private static readonly Counter<long> ProductsRetrievedCounter = Meter.CreateCounter<long>("products_retrieved_total", description: "Total products retrieved");
        private static readonly Histogram<double> ProductOperationDuration = Meter.CreateHistogram<double>("product_operation_duration_seconds", description: "Product operation duration in seconds");

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductService"/> class.
        /// </summary>
        /// <param name="productRepository">The product repository.</param>
        /// <param name="outboxRepository">The outbox repository for domain events.</param>
        /// <param name="cache">The cache adapter for reducing database load.</param>
        public ProductService(IRepository<Product> productRepository, IOutboxRepository outboxRepository, ICache cache)
        {
            _productRepository = productRepository;
            _outboxRepository = outboxRepository;
            _cache = cache;
        }

        /// <summary>
        /// Gets all products.
        /// </summary>
        /// <returns>A collection of all products.</returns>
        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            using var activity = ActivitySource.StartActivity("ProductService.GetAllAsync");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Try cache first
                var cached = await _cache.GetAsync<IEnumerable<Product>>(ProductListKey);
                if (cached != null)
                {
                    activity?.SetTag("cache.hit", true);
                    ProductsRetrievedCounter.Add(1, new KeyValuePair<string, object?>("operation", "get_all"), new KeyValuePair<string, object?>("cache_hit", true));
                    return cached;
                }

                activity?.SetTag("cache.hit", false);
                // Cache miss - read from DB and cache
                var products = await _productRepository.GetAllAsync();
                await _cache.SetAsync(ProductListKey, products, CacheTtl);

                ProductsRetrievedCounter.Add(products.Count(), new KeyValuePair<string, object?>("operation", "get_all"), new KeyValuePair<string, object?>("cache_hit", false));
                return products;
            }
            finally
            {
                stopwatch.Stop();
                ProductOperationDuration.Record(stopwatch.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("operation", "get_all"));
            }
        }

        /// <summary>
        /// Gets a product by its ID.
        /// </summary>
        /// <param name="id">The product ID.</param>
        /// <returns>The product if found, otherwise null.</returns>
        public async Task<Product?> GetByIdAsync(Guid id)
        {
            var cacheKey = GetProductKey(id);

            // Try cache first
            var cached = await _cache.GetAsync<Product>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            // Cache miss - read from DB and cache
            var product = await _productRepository.GetByIdAsync(id);
            if (product != null)
            {
                await _cache.SetAsync(cacheKey, product, CacheTtl);
            }
            return product;
        }

        /// <summary>
        /// Lists products with pagination.
        /// </summary>
        /// <param name="limit">The maximum number of items to return.</param>
        /// <param name="cursor">The cursor for pagination.</param>
        /// <returns>A tuple containing the items and the next cursor.</returns>
        public async Task<(IEnumerable<Product> Items, string? NextCursor)> ListPagedAsync(int limit, string? cursor)
        {
            // Clamp limit between 1 and 100
            if (limit < 1) limit = 1;
            if (limit > 100) limit = 100;
            return await _productRepository.ListPagedAsync(limit, cursor);
        }

        /// <summary>
        /// Adds a new product.
        /// </summary>
        /// <param name="dto">The product data transfer object.</param>
        /// <returns>The created product.</returns>
        public async Task<Product> AddAsync(ProductDto dto)
        {
            using var activity = ActivitySource.StartActivity("ProductService.AddAsync");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("product.name", dto.Name);
                activity?.SetTag("product.price", dto.Price);

                var product = Product.Create(Guid.NewGuid(), dto.Name!, dto.Description, dto.Price);
                await _productRepository.AddAsync(product);
                
                // Add domain events to outbox
                foreach (var domainEvent in product.DomainEvents)
                {
                    await _outboxRepository.AddAsync(domainEvent);
                }
                product.ClearDomainEvents();

                // Cache the new product and invalidate list cache
                await _cache.SetAsync(GetProductKey(product.Id), product, CacheTtl);
                await _cache.RemoveAsync(ProductListKey);

                ProductsCreatedCounter.Add(1);
                return product;
            }
            finally
            {
                stopwatch.Stop();
                ProductOperationDuration.Record(stopwatch.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("operation", "add"));
            }
        }

        /// <summary>
        /// Updates an existing product.
        /// </summary>
        /// <param name="id">The product ID.</param>
        /// <param name="dto">The product data transfer object.</param>
        public async Task UpdateAsync(Guid id, ProductDto dto)
        {
            using var activity = ActivitySource.StartActivity("ProductService.UpdateAsync");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("product.id", id);
                activity?.SetTag("product.name", dto.Name);
                activity?.SetTag("product.price", dto.Price);

                var product = await _productRepository.GetByIdAsync(id);
                if (product == null)
                {
                    return;
                }

                product.Update(dto.Name!, dto.Description, dto.Price);
                await _productRepository.UpdateAsync(product);

                // Add domain events to outbox
                foreach (var domainEvent in product.DomainEvents)
                {
                    await _outboxRepository.AddAsync(domainEvent);
                }
                product.ClearDomainEvents();

                // Invalidate caches
                await _cache.SetAsync(GetProductKey(id), product, CacheTtl);
                await _cache.RemoveAsync(ProductListKey);

                ProductsRetrievedCounter.Add(1, new KeyValuePair<string, object?>("operation", "update"));
            }
            finally
            {
                stopwatch.Stop();
                ProductOperationDuration.Record(stopwatch.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("operation", "update"));
            }
        }

        /// <summary>
        /// Deactivates a product (soft delete).
        /// </summary>
        /// <param name="id">The product ID.</param>
        /// <param name="reason">Optional reason for deactivation.</param>
        /// <returns>True if the product was deactivated, otherwise false.</returns>
        public async Task<bool> DeactivateAsync(Guid id, string? reason = null)
        {
            using var activity = ActivitySource.StartActivity("ProductService.DeactivateAsync");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("product.id", id);
                activity?.SetTag("product.deactivation_reason", reason);

                var product = await _productRepository.GetByIdAsync(id);
                if (product == null)
                {
                    return false;
                }

                product.Deactivate(reason);
                await _productRepository.UpdateAsync(product);

                // Add domain events to outbox
                foreach (var domainEvent in product.DomainEvents)
                {
                    await _outboxRepository.AddAsync(domainEvent);
                }
                product.ClearDomainEvents();

                // Invalidate caches
                await _cache.RemoveAsync(GetProductKey(id));
                await _cache.RemoveAsync(ProductListKey);

                return true;
            }
            finally
            {
                stopwatch.Stop();
                ProductOperationDuration.Record(stopwatch.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("operation", "deactivate"));
            }
        }
    }
}