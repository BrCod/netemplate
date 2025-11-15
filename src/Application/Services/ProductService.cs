using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using System.Collections.Generic;

namespace Application.Services
{
    public class ProductService
    {
        private readonly IRepository<Product> _productRepository;

        public ProductService(IRepository<Product> productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<IEnumerable<Product>> GetAllAsync() => await _productRepository.GetAllAsync();
        public async Task<Product?> GetByIdAsync(Guid id) => await _productRepository.GetByIdAsync(id);
        public async Task<Product> AddAsync(ProductDto dto)
        {
            var product = new Product { Id = Guid.NewGuid(), Name = dto.Name, Price = dto.Price };
            await _productRepository.AddAsync(product);
            return product;
        }
        public async Task UpdateAsync(Guid id, ProductDto dto)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return;
            product.Name = dto.Name;
            product.Price = dto.Price;
            await _productRepository.UpdateAsync(product);
        }
        public async Task DeleteAsync(Guid id)
        {
            await _productRepository.DeleteAsync(id);
        }
    }
}
