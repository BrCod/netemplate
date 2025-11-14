using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Postgres.Repositories
{
    /// <summary>
    /// Provides data access for products.
    /// </summary>
    public class ProductRepository : IRepository<Product>
    {
        private readonly AppDbContext _context;
        /// <summary>
        /// Initializes a new instance of the <see cref="ProductRepository"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a product by its identifier asynchronously.
        /// </summary>
        /// <param name="id">The product identifier.</param>
        /// <returns>The product, or null if not found.</returns>
        public async Task<Product?> GetByIdAsync(Guid id) => await _context.Products.FindAsync(id);

        /// <summary>
        /// Retrieves all products asynchronously.
        /// </summary>
        /// <returns>A list of products.</returns>
        public async Task<IEnumerable<Product>> GetAllAsync() => await _context.Products.ToListAsync();

        /// <summary>
        /// Adds a new product asynchronously.
        /// </summary>
        /// <param name="entity">The product to add.</param>
        /// <returns>A task representing the operation.</returns>
        public async Task AddAsync(Product entity)
        {
            _context.Products.Add(entity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Updates an existing product asynchronously.
        /// </summary>
        /// <param name="entity">The product to update.</param>
        /// <returns>A task representing the operation.</returns>
        public async Task UpdateAsync(Product entity)
        {
            _context.Products.Update(entity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a product by its identifier asynchronously.
        /// </summary>
        /// <param name="id">The product identifier.</param>
        /// <returns>A task representing the operation.</returns>
        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Products.FindAsync(id);
            if (entity != null)
            {
                _context.Products.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}
