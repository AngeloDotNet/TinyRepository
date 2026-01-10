using System.Linq.Expressions;
using TinyRepository.Entities;
using TinyRepository.Paging;

namespace TinyRepository.Interfaces;

public interface IRepository<T, TKey>
        where T : class, IEntity<TKey>
        where TKey : IEquatable<TKey>
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

    // Patch: apply a small change without replacing the whole object
    /// <summary>
    /// Finds the entity by id (tracked), applies the provided patch action and marks it modified.
    /// Returns true if entity found and patched; false if not found.
    /// </summary>
    Task<bool> PatchAsync(TKey id, Action<T> patchAction, CancellationToken cancellationToken = default);

    // Expose queryable for advanced queries (caller responsible for materialization)
    /// <summary>
    /// Returns an IQueryable<T> to allow callers to build complex queries. If asNoTracking is true, the returned query has AsNoTracking applied.
    /// </summary>
    IQueryable<T> Query(bool asNoTracking = true);

    IQueryable<T> Query(Expression<Func<T, bool>> predicate, bool asNoTracking = true);

    // Paging with optional filter and ordering function
    /// <summary>
    /// Returns a PagedResult containing items for the specified page and the total count matching the filter.
    /// Use orderBy to provide ordering: q => q.OrderBy(x => x.Name) or q => q.OrderByDescending(...)
    /// If orderBy is null the underlying provider will not apply a deterministic order (use at your risk).
    /// </summary>
    Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Expression<Func<T, bool>>? filter = null, bool asNoTracking = true, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);
}