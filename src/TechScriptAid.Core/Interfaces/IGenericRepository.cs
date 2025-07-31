using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TechScriptAid.Core.Common;
using TechScriptAid.Core.Entities;
using TechScriptAid.Core.Specifications;

namespace TechScriptAid.Core.Interfaces
{
    public interface IGenericRepository<T> where T : BaseEntity
    {
        // Basic CRUD operations
        Task<T> GetByIdAsync(Guid id);
        Task<IReadOnlyList<T>> GetAllAsync();
        Task<T> AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);
        void Delete(T entity);
        void DeleteRange(IEnumerable<T> entities);

        // Specification pattern support
        Task<T> GetEntityWithSpec(ISpecification<T> spec);
        Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec);
        Task<int> CountAsync(ISpecification<T> spec);

        // Advanced query operations
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);

        // Pagination support
        Task<PaginatedList<T>> GetPaginatedAsync(int pageNumber, int pageSize);
        Task<PaginatedList<T>> GetPaginatedAsync(Expression<Func<T, bool>> predicate, int pageNumber, int pageSize);
        Task<PaginatedList<T>> GetPaginatedAsync(ISpecification<T> spec, int pageNumber, int pageSize);

        // Include support
        Task<T> GetByIdWithIncludeAsync(Guid id, params Expression<Func<T, object>>[] includeProperties);
        Task<IReadOnlyList<T>> GetAllWithIncludeAsync(params Expression<Func<T, object>>[] includeProperties);

        // Bulk operations
        Task<int> ExecuteDeleteAsync(Expression<Func<T, bool>> predicate);
        //Task<int> ExecuteUpdateAsync(
        //    Expression<Func<T, bool>> predicate,
        //    Expression<Func<Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<T>,
        //        Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<T>>> setPropertyCalls);

        //Task<int> ExecuteUpdateAsync(Expression<Func<T, bool>> predicate, Action<T> updateAction);
        // Soft delete support
        Task<T> GetByIdIncludingDeletedAsync(Guid id);
        Task<IReadOnlyList<T>> GetAllIncludingDeletedAsync();
        void Restore(T entity);

        // Async enumerable support
        IAsyncEnumerable<T> AsAsyncEnumerable(Expression<Func<T, bool>> predicate = null);
    }
}