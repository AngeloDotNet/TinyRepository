using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TinyRepository.Entities;
using TinyRepository.Interfaces;
using TinyRepository.Paging;

namespace TinyRepository;

public class EfRepository<T, TKey> : IRepository<T, TKey>
        where T : class, IEntity<TKey>
        where TKey : IEquatable<TKey>
{
    protected readonly DbContext _context;
    protected readonly DbSet<T> _dbSet;

    public EfRepository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        // FindAsync returns tracked entity by default
        return await _dbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public virtual Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        // EF Core does not have an async UpdateRange; updating entity states is in-memory and quick.
        _dbSet.UpdateRange(entities);

        return Task.CompletedTask;
    }

    public virtual void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        // Removing ranges is in-memory (marking entities Deleted). Return completed task for async signature.
        _dbSet.RemoveRange(entities);
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
        if (patchAction == null)
        {
            throw new ArgumentNullException(nameof(patchAction));
        }

        // Use FindAsync to get a tracked entity (so changes are tracked)
        var entity = await _dbSet.FindAsync(new object[] { id }, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        patchAction(entity);

        // Mark modified to ensure changes are persisted even if change tracking misses something
        _context.Entry(entity).State = EntityState.Modified;
        return true;
    }

    public virtual IQueryable<T> Query(bool asNoTracking = true)
    {
        return asNoTracking ? _dbSet.AsNoTracking() : _dbSet;
    }

    public virtual IQueryable<T> Query(Expression<Func<T, bool>> predicate, bool asNoTracking = true)
    {
        var q = asNoTracking ? _dbSet.AsNoTracking() : _dbSet;

        return q.Where(predicate);
    }

    public virtual async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Expression<Func<T, bool>>? filter = null, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var query = asNoTracking ? _dbSet.AsNoTracking().AsQueryable() : _dbSet.AsQueryable();

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

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);

        return entity is not null;
    }
}