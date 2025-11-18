using Microsoft.EntityFrameworkCore;
using Netemplate.Application.Interfaces;
using Netemplate.Domain.Entities;

namespace Netemplate.Infrastructure.Persistence.Postgres;

public sealed class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Products.FindAsync(new object[] { id }, ct);
    }

    public async Task<Product?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Name.Value == name, ct);
    }

    public async Task<IReadOnlyList<Product>> ListAsync(int skip = 0, int take = 50, CancellationToken ct = default)
    {
        return await _context.Products
            .OrderBy(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Product entity, CancellationToken ct = default)
    {
        await _context.Products.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Product entity, CancellationToken ct = default)
    {
        _context.Products.Update(entity);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Product entity, CancellationToken ct = default)
    {
        entity.Deactivate();
        _context.Products.Update(entity);
        await _context.SaveChangesAsync(ct);
    }
}
