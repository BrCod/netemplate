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
        /// <summary>Add a new entity.</summary>
        Task AddAsync(T entity);
        /// <summary>Update an existing entity.</summary>
        Task UpdateAsync(T entity);
        /// <summary>Delete entity by ID.</summary>
        Task DeleteAsync(Guid id);
    }
}
