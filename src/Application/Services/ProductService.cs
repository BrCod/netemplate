using Netemplate.Application.DTOs;
using Netemplate.Application.Interfaces;
using Netemplate.Domain.Entities;
using Netemplate.Domain.Results;
using Netemplate.Domain.ValueObjects;

namespace Netemplate.Application.Services;

public interface IProductService
{
    Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProductListResponse>> ListAsync(int skip = 0, int take = 50, CancellationToken ct = default);
    Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ICache _cache;
    private readonly IEventPublisher _eventPublisher;
    private readonly IFeatureFlagService _featureFlags;

    public ProductService(
        IProductRepository repository,
        ICache cache,
        IEventPublisher eventPublisher,
        IFeatureFlagService featureFlags)
    {
        _repository = repository;
        _cache = cache;
        _eventPublisher = eventPublisher;
        _featureFlags = featureFlags;
    }

    public async Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cacheEnabled = await _featureFlags.IsEnabledAsync("ProductCaching", ct);
        
        if (cacheEnabled)
        {
            var cached = await _cache.GetAsync<ProductDto>($"product:{id}", ct);
            if (cached != null)
                return Result<ProductDto>.Success(cached);
        }

        var product = await _repository.GetByIdAsync(id, ct);
        if (product == null)
            return Result<ProductDto>.Failure("Product not found");

        var dto = MapToDto(product);

        if (cacheEnabled)
        {
            await _cache.SetAsync($"product:{id}", dto, TimeSpan.FromMinutes(5), ct);
        }

        return Result<ProductDto>.Success(dto);
    }

    public async Task<Result<ProductListResponse>> ListAsync(int skip = 0, int take = 50, CancellationToken ct = default)
    {
        var products = await _repository.ListAsync(skip, take, ct);
        var dtos = products.Select(MapToDto).ToList();

        var response = new ProductListResponse(dtos, dtos.Count, skip, take);
        return Result<ProductListResponse>.Success(response);
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        try
        {
            var name = ProductName.Create(request.Name);
            var price = Money.Create(request.Price, request.Currency);
            var product = Product.Create(name, price, request.Description);
        
            await _repository.AddAsync(product, ct);

            var publishEnabled = await _featureFlags.IsEnabledAsync("EventPublishing", ct);
            if (publishEnabled)
            {
                foreach (var domainEvent in product.DomainEvents)
                {
                    await _eventPublisher.PublishAsync(domainEvent, ct);
                }
            }

            product.ClearDomainEvents();

            var dto = MapToDto(product);
            return Result<ProductDto>.Success(dto);
        }
        catch (ArgumentException ex)
        {
            return Result<ProductDto>.Failure(ex.Message);
        }
    }

    public async Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        try
        {
            var product = await _repository.GetByIdAsync(id, ct);
            if (product == null)
                return Result<ProductDto>.Failure("Product not found");

            ProductName? name = null;
            Money? price = null;

            if (request.Name != null)
            {
                name = ProductName.Create(request.Name);
            }

            if (request.Price.HasValue && request.Currency != null)
            {
                price = Money.Create(request.Price.Value, request.Currency);
            }

            product.Update(name, price, request.Description);
            await _repository.UpdateAsync(product, ct);

            var publishEnabled = await _featureFlags.IsEnabledAsync("EventPublishing", ct);
            if (publishEnabled)
            {
                foreach (var domainEvent in product.DomainEvents)
                {
                    await _eventPublisher.PublishAsync(domainEvent, ct);
                }
            }

            product.ClearDomainEvents();

            // Invalidate cache
            var cacheEnabled = await _featureFlags.IsEnabledAsync("ProductCaching", ct);
            if (cacheEnabled)
            {
                await _cache.RemoveAsync($"product:{id}", ct);
            }

            var dto = MapToDto(product);
            return Result<ProductDto>.Success(dto);
        }
        catch (ArgumentException ex)
        {
            return Result<ProductDto>.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(id, ct);
        if (product == null)
            return Result.Failure("Product not found");

        await _repository.DeleteAsync(product, ct);

        var publishEnabled = await _featureFlags.IsEnabledAsync("EventPublishing", ct);
        if (publishEnabled)
        {
            foreach (var domainEvent in product.DomainEvents)
            {
                await _eventPublisher.PublishAsync(domainEvent, ct);
            }
        }

        product.ClearDomainEvents();

        // Invalidate cache
        var cacheEnabled = await _featureFlags.IsEnabledAsync("ProductCaching", ct);
        if (cacheEnabled)
        {
            await _cache.RemoveAsync($"product:{id}", ct);
        }

        return Result.Success();
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto(
            product.Id,
            product.Name.Value,
            product.Price.Amount,
            product.Price.Currency,
            product.Description,
            product.CreatedAt,
            product.UpdatedAt,
            product.IsActive
        );
    }
}
