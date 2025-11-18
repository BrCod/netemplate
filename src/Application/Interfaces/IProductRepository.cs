using Netemplate.Domain.Entities;

namespace Netemplate.Application.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetByNameAsync(string name, CancellationToken ct = default);
}
