using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TinyRepository.Entities;
using TinyRepository.Extensions;
using TinyRepository.Interfaces;
using TinyRepository.Paging;
using TinyRepository.Provider;
using TinyRepository.Sorting;

namespace TinyRepository;

public class EfRepository<T, TKey> : IRepository<T, TKey>
        where T : class, IEntity<TKey>
        where TKey : IEquatable<TKey>
{
    protected readonly DbContext context;
    protected readonly DbSet<T> dbSet;
    private readonly IEnumerable<string>? allowedProperties; // whitelist for ordering (may be null)

    public EfRepository(DbContext context, IServiceProvider serviceProvider)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        dbSet = this.context.Set<T>();

        // attempt to get whitelist provider from DI (if registered)
        try
        {
            var provider = serviceProvider?.GetService<IPropertyWhitelistProvider<T>>();
            if (provider != null)
            {
                allowedProperties = provider.GetAllowedProperties();
            }
            else
            {
                // fallback: scan attributes [Orderable] on T and related types
                var scanned = OrderablePropertyScanner.GetOrderableProperties(typeof(T));
                allowedProperties = scanned.Any() ? scanned : null;
            }
        }
        catch
        {
            allowedProperties = null;
        }
    }

    public virtual async Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default) => await dbSet.FindAsync([id], cancellationToken);
    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default) => await dbSet.AsNoTracking().ToListAsync(cancellationToken);
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default) => await dbSet.AddAsync(entity, cancellationToken);
    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) => await dbSet.AddRangeAsync(entities, cancellationToken);

    public virtual void Update(T entity) => dbSet.Update(entity);
    public virtual Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        dbSet.UpdateRange(entities);
        return Task.CompletedTask;
    }

    public virtual void Remove(T entity) => dbSet.Remove(entity);
    public virtual Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        dbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }
    public virtual async Task RemoveByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);

        if (entity is not null)
        {
            Remove(entity);
        }
    }

    public virtual async Task<bool> PatchAsync(TKey id, Action<T> patchAction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patchAction);

        var entity = await dbSet.FindAsync([id], cancellationToken);

        if (entity is null)
        {
            return false;
        }

        patchAction(entity);
        context.Entry(entity).State = EntityState.Modified;
        {
            return true;
        }
    }

    public virtual IQueryable<T> Query(bool asNoTracking = true) => asNoTracking ? dbSet.AsNoTracking() : dbSet;
    public virtual IQueryable<T> Query(bool asNoTracking = true, params Expression<Func<T, object>>[] includes)
    {
        var q = asNoTracking ? dbSet.AsNoTracking() : dbSet;
        return q.IncludeMultiple(includes);
    }
    public virtual IQueryable<T> Query(bool asNoTracking = true, params string[] includePaths)
    {
        var q = asNoTracking ? dbSet.AsNoTracking() : dbSet;
        return q.IncludePaths(includePaths);
    }

    public virtual IQueryable<T> Query(Expression<Func<T, bool>> predicate, bool asNoTracking = true)
    {
        var q = asNoTracking ? dbSet.AsNoTracking() : dbSet;
        return q.Where(predicate);
    }
    public virtual IQueryable<T> Query(Expression<Func<T, bool>> predicate, bool asNoTracking = true, params Expression<Func<T, object>>[] includes)
    {
        var q = asNoTracking ? dbSet.AsNoTracking() : dbSet;
        q = q.Where(predicate);
        return q.IncludeMultiple(includes);
    }
    public virtual IQueryable<T> Query(Expression<Func<T, bool>> predicate, bool asNoTracking = true, params string[] includePaths)
    {
        var q = asNoTracking ? dbSet.AsNoTracking() : dbSet;
        q = q.Where(predicate);
        return q.IncludePaths(includePaths);
    }

    public virtual async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Expression<Func<T, bool>>? filter = null, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var query = asNoTracking ? dbSet.AsNoTracking().AsQueryable() : dbSet.AsQueryable();

        if (filter is not null)
        {
            query = query.Where(filter);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (orderBy is not null)
        {
            query = orderBy(query);
        }

        var skip = (pageNumber - 1) * pageSize;
        var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
    }

    public virtual async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, string? orderByProperty, bool descending = false,
        Expression<Func<T, bool>>? filter = null, bool asNoTracking = true, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var query = asNoTracking ? dbSet.AsNoTracking().AsQueryable() : dbSet.AsQueryable();

        query = query.IncludeMultiple(includes);

        if (filter is not null)
        {
            query = query.Where(filter);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(orderByProperty))
        {
            // se esiste whitelist controlliamo
            if (allowedProperties != null)
            {
                var allowedSet = new HashSet<string>(allowedProperties, StringComparer.OrdinalIgnoreCase);
                if (!allowedSet.Contains(orderByProperty))
                {
                    throw new ArgumentException($"Property '{orderByProperty}' is not allowed for ordering.");
                }
            }

            query = query.ApplyOrderByProperty(orderByProperty, descending);
        }

        var skip = (pageNumber - 1) * pageSize;
        var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
    }
    public virtual async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, string? orderByProperty, bool descending = false,
        Expression<Func<T, bool>>? filter = null, bool asNoTracking = true, CancellationToken cancellationToken = default, params string[] includePaths)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var query = asNoTracking ? dbSet.AsNoTracking().AsQueryable() : dbSet.AsQueryable();

        query = query.IncludePaths(includePaths);

        if (filter is not null)
            query = query.Where(filter);

        var totalCount = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(orderByProperty))
        {
            if (allowedProperties != null)
            {
                var allowedSet = new HashSet<string>(allowedProperties, StringComparer.OrdinalIgnoreCase);
                if (!allowedSet.Contains(orderByProperty))
                    throw new ArgumentException($"Property '{orderByProperty}' is not allowed for ordering.");
            }

            query = query.ApplyOrderByProperty(orderByProperty, descending);
        }

        var skip = (pageNumber - 1) * pageSize;
        var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
    }

    public virtual async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, IEnumerable<SortDescriptor>? sortDescriptors,
        Expression<Func<T, bool>>? filter = null, bool asNoTracking = true, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var query = asNoTracking ? dbSet.AsNoTracking().AsQueryable() : dbSet.AsQueryable();

        query = query.IncludeMultiple(includes);

        if (filter is not null)
        {
            query = query.Where(filter);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (sortDescriptors != null)
        {
            query = query.ApplyOrdering(sortDescriptors, allowedProperties);
        }

        var skip = (pageNumber - 1) * pageSize;
        var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
    }
    public virtual async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, IEnumerable<SortDescriptor>? sortDescriptors,
        Expression<Func<T, bool>>? filter = null, bool asNoTracking = true, CancellationToken cancellationToken = default, params string[] includePaths)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var query = asNoTracking ? dbSet.AsNoTracking().AsQueryable() : dbSet.AsQueryable();

        query = query.IncludePaths(includePaths);

        if (filter is not null)
            query = query.Where(filter);

        var totalCount = await query.CountAsync(cancellationToken);

        if (sortDescriptors != null)
        {
            query = query.ApplyOrdering(sortDescriptors, allowedProperties);
        }

        var skip = (pageNumber - 1) * pageSize;
        var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default) => await dbSet.CountAsync(cancellationToken);
    public virtual async Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        return entity is not null;
    }
}