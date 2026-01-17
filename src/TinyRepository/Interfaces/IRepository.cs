using System.Linq.Expressions;
using TinyRepository.Entities;
using TinyRepository.Paging;
using TinyRepository.Sorting;

namespace TinyRepository.Interfaces;

public interface IRepository<T, TKey> where T : class, IEntity<TKey> where TKey : IEquatable<TKey>
{
    // Basic CRUD
    Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    void Update(T entity);
    Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    void Remove(T entity);
    Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task RemoveByIdAsync(TKey id, CancellationToken cancellationToken = default);

    // Patch
    Task<bool> PatchAsync(TKey id, Action<T> patchAction, CancellationToken cancellationToken = default);

    // Expose queryable for advanced queries (caller responsible for materialization)
    IQueryable<T> Query(bool asNoTracking = true);
    IQueryable<T> Query(bool asNoTracking = true, params Expression<Func<T, object>>[] includes);

    IQueryable<T> Query(Expression<Func<T, bool>> predicate, bool asNoTracking = true);
    IQueryable<T> Query(Expression<Func<T, bool>> predicate, bool asNoTracking = true, params Expression<Func<T, object>>[] includes);

    // Paging with optional filter and ordering function or dynamic ordering by property name
    Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Expression<Func<T, bool>>? filter = null, bool asNoTracking = true, CancellationToken cancellationToken = default);

    // Get paged with dynamic ordering by property name (supports nested properties "Author.Name")
    Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, string? orderByProperty, bool descending = false, Expression<Func<T, bool>>? filter = null,
        bool asNoTracking = true, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);

    // Get paged with multiple sort descriptors (property + direction)
    Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, IEnumerable<SortDescriptor>? sortDescriptors, Expression<Func<T, bool>>? filter = null,
        bool asNoTracking = true, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);
}