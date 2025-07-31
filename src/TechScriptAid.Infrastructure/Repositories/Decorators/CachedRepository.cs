using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TechScriptAid.Core.Common;
using TechScriptAid.Core.Entities;
using TechScriptAid.Core.Interfaces;
using TechScriptAid.Core.Specifications;

namespace TechScriptAid.Infrastructure.Repositories.Decorators
{
    public class CachedRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        private readonly IGenericRepository<T> _repository;
        private readonly IMemoryCache _cache;
        private readonly MemoryCacheEntryOptions _cacheOptions;
        private readonly string _cacheKeyPrefix;

        public CachedRepository(
            IGenericRepository<T> repository,
            IMemoryCache cache,
            TimeSpan? slidingExpiration = null)
        {
            _repository = repository;
            _cache = cache;
            _cacheKeyPrefix = $"{typeof(T).Name}_";

            _cacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = slidingExpiration ?? TimeSpan.FromMinutes(5),
                Priority = CacheItemPriority.Normal
            };
        }

        public async Task<T> GetByIdAsync(Guid id)
        {
            var cacheKey = $"{_cacheKeyPrefix}{id}";

            if (_cache.TryGetValue<T>(cacheKey, out var cachedEntity))
            {
                return cachedEntity;
            }

            var entity = await _repository.GetByIdAsync(id);

            if (entity != null)
            {
                _cache.Set(cacheKey, entity, _cacheOptions);
            }

            return entity;
        }

        public async Task<IReadOnlyList<T>> GetAllAsync()
        {
            var cacheKey = $"{_cacheKeyPrefix}All";

            if (_cache.TryGetValue<IReadOnlyList<T>>(cacheKey, out var cachedList))
            {
                return cachedList;
            }

            var list = await _repository.GetAllAsync();
            _cache.Set(cacheKey, list, _cacheOptions);

            return list;
        }

        public async Task<T> AddAsync(T entity)
        {
            var result = await _repository.AddAsync(entity);
            InvalidateCache();
            return result;
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _repository.AddRangeAsync(entities);
            InvalidateCache();
        }

        public void Update(T entity)
        {
            _repository.Update(entity);
            InvalidateEntityCache(entity.Id);
            InvalidateCollectionCache();
        }

        public void UpdateRange(IEnumerable<T> entities)
        {
            _repository.UpdateRange(entities);
            InvalidateCache();
        }

        public void Delete(T entity)
        {
            _repository.Delete(entity);
            InvalidateEntityCache(entity.Id);
            InvalidateCollectionCache();
        }

        public void DeleteRange(IEnumerable<T> entities)
        {
            _repository.DeleteRange(entities);
            InvalidateCache();
        }

        // Specification pattern methods - with selective caching
        public async Task<T> GetEntityWithSpec(ISpecification<T> spec)
        {
            // Complex specifications are not cached by default
            return await _repository.GetEntityWithSpec(spec);
        }

        public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec)
        {
            // Could implement spec-based caching with careful key generation
            return await _repository.ListAsync(spec);
        }

        public async Task<int> CountAsync(ISpecification<T> spec)
        {
            return await _repository.CountAsync(spec);
        }

        // Advanced query operations
        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _repository.ExistsAsync(predicate);
        }

        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _repository.FirstOrDefaultAsync(predicate);
        }

        public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _repository.FindAsync(predicate);
        }

        // Pagination support
        public async Task<PaginatedList<T>> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            var cacheKey = $"{_cacheKeyPrefix}Page_{pageNumber}_{pageSize}";

            if (_cache.TryGetValue<PaginatedList<T>>(cacheKey, out var cachedPage))
            {
                return cachedPage;
            }

            var page = await _repository.GetPaginatedAsync(pageNumber, pageSize);
            _cache.Set(cacheKey, page, _cacheOptions);

            return page;
        }

        public async Task<PaginatedList<T>> GetPaginatedAsync(
            Expression<Func<T, bool>> predicate,
            int pageNumber,
            int pageSize)
        {
            return await _repository.GetPaginatedAsync(predicate, pageNumber, pageSize);
        }

        public async Task<PaginatedList<T>> GetPaginatedAsync(
            ISpecification<T> spec,
            int pageNumber,
            int pageSize)
        {
            return await _repository.GetPaginatedAsync(spec, pageNumber, pageSize);
        }

        // Include support
        public async Task<T> GetByIdWithIncludeAsync(
            Guid id,
            params Expression<Func<T, object>>[] includeProperties)
        {
            return await _repository.GetByIdWithIncludeAsync(id, includeProperties);
        }

        public async Task<IReadOnlyList<T>> GetAllWithIncludeAsync(
            params Expression<Func<T, object>>[] includeProperties)
        {
            return await _repository.GetAllWithIncludeAsync(includeProperties);
        }

        // Bulk operations
        public async Task<int> ExecuteDeleteAsync(Expression<Func<T, bool>> predicate)
        {
            var result = await _repository.ExecuteDeleteAsync(predicate);
            InvalidateCache();
            return result;
        }

        //public async Task<int> ExecuteUpdateAsync(
        //    Expression<Func<T, bool>> predicate,
        //    Expression<Func<Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<T>,
        //        Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<T>>> setPropertyCalls)
        //{
        //    var result = await _repository.ExecuteUpdateAsync(predicate, setPropertyCalls);
        //    InvalidateCache();
        //    return result;
        //}

        // Soft delete support
        public async Task<T> GetByIdIncludingDeletedAsync(Guid id)
        {
            return await _repository.GetByIdIncludingDeletedAsync(id);
        }

        public async Task<IReadOnlyList<T>> GetAllIncludingDeletedAsync()
        {
            return await _repository.GetAllIncludingDeletedAsync();
        }

        public void Restore(T entity)
        {
            _repository.Restore(entity);
            InvalidateEntityCache(entity.Id);
            InvalidateCollectionCache();
        }

        // Async enumerable support
        public IAsyncEnumerable<T> AsAsyncEnumerable(Expression<Func<T, bool>> predicate = null)
        {
            return _repository.AsAsyncEnumerable(predicate);
        }

        // Cache management methods
        private void InvalidateCache()
        {
            var pattern = $"{_cacheKeyPrefix}*";
            // Note: IMemoryCache doesn't support pattern removal
            // In production, consider using IDistributedCache with Redis
            InvalidateCollectionCache();
        }

        private void InvalidateEntityCache(Guid id)
        {
            _cache.Remove($"{_cacheKeyPrefix}{id}");
        }

        private void InvalidateCollectionCache()
        {
            _cache.Remove($"{_cacheKeyPrefix}All");
            // Also remove paginated results
            // In production, track cache keys for removal
        }
    }
}
