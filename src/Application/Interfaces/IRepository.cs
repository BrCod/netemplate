using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Generic repository interface for CRUD operations.
    /// </summary>
    public interface IRepository<T>
    {
        /// <summary>Get entity by ID.</summary>
        Task<T?> GetByIdAsync(Guid id);
        /// <summary>Get all entities.</summary>
        Task<IEnumerable<T>> GetAllAsync();
        /// <summary>Get a paged set of entities using a cursor strategy.</summary>
        /// <param name="limit">Maximum number of items to return (bounded 1-100).</param>
        /// <param name="cursor">Opaque cursor (e.g., last item ID) or null for start.</param>
        /// <returns>Tuple of items and next cursor (null if end).</returns>
        Task<(IEnumerable<T> Items, string? NextCursor)> ListPagedAsync(int limit, string? cursor);
        /// <summary>Add a new entity.</summary>
        Task AddAsync(T entity);
        /// <summary>Update an existing entity.</summary>
        Task UpdateAsync(T entity);
        /// <summary>Delete entity by ID.</summary>
        Task DeleteAsync(Guid id);
    }
}
