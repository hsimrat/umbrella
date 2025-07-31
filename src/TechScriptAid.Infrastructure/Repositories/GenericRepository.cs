using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TechScriptAid.Core.Common;
using TechScriptAid.Core.Entities;
using TechScriptAid.Core.Interfaces;
using TechScriptAid.Core.Specifications;
using TechScriptAid.Infrastructure.Data;
using TechScriptAid.Infrastructure.Specifications;
using TechScriptAid.Infrastructure.Helpers;

namespace TechScriptAid.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        // Basic CRUD operations
        public virtual async Task<T> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IReadOnlyList<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public virtual void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public virtual void UpdateRange(IEnumerable<T> entities)
        {
            _dbSet.UpdateRange(entities);
        }

        public virtual void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public virtual void DeleteRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        // Specification pattern support
        public async Task<T> GetEntityWithSpec(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).ToListAsync();
        }

        public async Task<int> CountAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).CountAsync();
        }

        // Advanced query operations
        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        // Pagination support
        // Update the GetPaginatedAsync method to use PaginationHelper
        public async Task<PaginatedList<T>> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            return await PaginationHelper.CreateAsync(_dbSet, pageNumber, pageSize);
        }

        public async Task<PaginatedList<T>> GetPaginatedAsync(
            Expression<Func<T, bool>> predicate,
            int pageNumber,
            int pageSize)
        {
            var query = _dbSet.Where(predicate);
            return await PaginationHelper.CreateAsync(query, pageNumber, pageSize);
        }

        public async Task<PaginatedList<T>> GetPaginatedAsync(
            ISpecification<T> spec,
            int pageNumber,
            int pageSize)
        {
            var query = ApplySpecification(spec);
            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedList<T>(items, totalCount, pageNumber, pageSize);
        }

        // Include support
        public async Task<T> GetByIdWithIncludeAsync(
            Guid id,
            params Expression<Func<T, object>>[] includeProperties)
        {
            var query = _dbSet.AsQueryable();

            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }

            return await query.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IReadOnlyList<T>> GetAllWithIncludeAsync(
            params Expression<Func<T, object>>[] includeProperties)
        {
            var query = _dbSet.AsQueryable();

            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }

            return await query.ToListAsync();
        }

        // Bulk operations
        public async Task<int> ExecuteDeleteAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ExecuteDeleteAsync();
        }

        // For ExecuteUpdateAsync, we'll implement it differently since we removed EF Core types from interface
        //public async Task<int> ExecuteUpdateAsync(
        //    Expression<Func<T, bool>> predicate,
        //    Action<T> updateAction)
        //{
        //    var entities = await _dbSet.Where(predicate).ToListAsync();
        //    foreach (var entity in entities)
        //    {
        //        updateAction(entity);
        //        _dbSet.Update(entity);
        //    }
        //    return entities.Count;
        //}

        // Soft delete support
        public async Task<T> GetByIdIncludingDeletedAsync(Guid id)
        {
            return await _dbSet
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IReadOnlyList<T>> GetAllIncludingDeletedAsync()
        {
            return await _dbSet
                .IgnoreQueryFilters()
                .ToListAsync();
        }

        public void Restore(T entity)
        {
            if (entity.IsDeleted)
            {
                entity.IsDeleted = false;
                entity.DeletedAt = null;
                entity.DeletedBy = null;
                Update(entity);
            }
        }

        // Helper method to apply specifications
        protected IQueryable<T> ApplySpecification(ISpecification<T> spec)
        {
            return SpecificationEvaluator<T>.GetQuery(_dbSet.AsQueryable(), spec);
        }

        // Async enumerable support
        public async IAsyncEnumerable<T> AsAsyncEnumerable(Expression<Func<T, bool>> predicate = null)
        {
            var query = predicate != null ? _dbSet.Where(predicate) : _dbSet;

            await foreach (var item in query.AsAsyncEnumerable())
            {
                yield return item;
            }
        }
    }
}
