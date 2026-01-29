using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TinyRepository.Entities;
using TinyRepository.Extensions;
using TinyRepository.Interfaces;
using TinyRepository.Paging;
using TinyRepository.Provider.Interfaces;
using TinyRepository.Sorting;

namespace TinyRepository;

public class EfRepository<T, TKey> : IRepository<T, TKey> where T : class, IEntity<TKey> where TKey : IEquatable<TKey>
{
    protected readonly DbContext dbContext;
    protected readonly DbSet<T> dbSet;

    private readonly IEnumerable<string>? allowedProperties; // sort whitelist
    private readonly IEnumerable<string>? allowedIncludePaths; // include whitelist

    public EfRepository(DbContext context, IServiceProvider serviceProvider)
    {
        dbContext = context ?? throw new ArgumentNullException(nameof(context));
        dbSet = dbContext.Set<T>();

        try
        {
            var providerProp = serviceProvider?.GetService<IPropertyWhitelistProvider<T>>();

            if (providerProp != null)
            {
                allowedProperties = providerProp.GetAllowedProperties();
            }
            else
            {
                var scanned = OrderablePropertyScanner.GetOrderableProperties(typeof(T));
                allowedProperties = scanned.Any() ? scanned : null;
            }

            var providerInclude = serviceProvider?.GetService<IIncludeWhitelistProvider<T>>();

            if (providerInclude != null)
            {
                allowedIncludePaths = providerInclude.GetAllowedIncludePaths();
            }
            else
            {
                var scannedInclude = IncludePathScanner.GetIncludePaths(typeof(T));
                allowedIncludePaths = scannedInclude.Any() ? scannedInclude : null;
            }
        }
        catch
        {
            allowedProperties = null;
            allowedIncludePaths = null;
        }
    }

    public virtual async Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        => await dbSet.FindAsync([id], cancellationToken);
    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbSet.AsNoTracking().ToListAsync(cancellationToken);
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await dbSet.AddAsync(entity, cancellationToken);
    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        => await dbSet.AddRangeAsync(entities, cancellationToken);

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
        dbContext.Entry(entity).State = EntityState.Modified;

        return true;
    }

    // Query overloads
    public virtual IQueryable<T> Query(bool asNoTracking = true) => asNoTracking ? dbSet.AsNoTracking() : dbSet;

    public virtual IQueryable<T> Query(bool asNoTracking = true, params Expression<Func<T, object>>[] includes)
    {
        var q = asNoTracking ? dbSet.AsNoTracking() : dbSet;
        return q.IncludeMultiple(includes);
    }

    public virtual IQueryable<T> Query(bool asNoTracking = true, params string[] includePaths)
    {
        var q = asNoTracking ? dbSet.AsNoTracking() : dbSet;
        if (includePaths != null && includePaths.Length > 0)
        {
            ValidateIncludePaths(includePaths);
            q = q.IncludePaths(includePaths);
        }

        return q;
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
        if (includePaths != null && includePaths.Length > 0)
        {
            ValidateIncludePaths(includePaths);
            q = q.IncludePaths(includePaths);
        }

        return q;
    }

    public virtual async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Expression<Func<T, bool>>? filter = null, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var query = asNoTracking ? dbSet.AsNoTracking().AsQueryable() : dbSet.AsQueryable();

        if (filter != null)
        {
            query = query.Where(filter);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (orderBy != null)
        {
            query = orderBy(query);
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

        if (includePaths != null && includePaths.Length > 0)
        {
            ValidateIncludePaths(includePaths);
        }

        var query = asNoTracking ? dbSet.AsNoTracking().AsQueryable() : dbSet.AsQueryable();
        if (includePaths != null && includePaths.Length > 0)
            query = query.IncludePaths(includePaths);

        if (filter != null)
            query = query.Where(filter);

        var totalCount = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(orderByProperty))
        {
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

    public virtual async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, IEnumerable<SortDescriptor>? sortDescriptors,
        Expression<Func<T, bool>>? filter = null, bool asNoTracking = true, CancellationToken cancellationToken = default, params string[] includePaths)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        if (includePaths != null && includePaths.Length > 0)
        {
            ValidateIncludePaths(includePaths);
        }

        var query = asNoTracking ? dbSet.AsNoTracking().AsQueryable() : dbSet.AsQueryable();

        if (includePaths != null && includePaths.Length > 0)
        {
            query = query.IncludePaths(includePaths);
        }

        if (filter != null)
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

    // Two-stage paging implementation: first select PKs (ids) with ordering+filter+skip/take, then load entities with includes filtering by ids.
    public virtual async Task<PagedResult<T>> GetPagedTwoStageAsync(int pageNumber, int pageSize, IEnumerable<SortDescriptor>? sortDescriptors = null,
        string? orderByProperty = null, bool descending = false, Expression<Func<T, bool>>? filter = null, bool asNoTracking = true,
        CancellationToken cancellationToken = default, params string[] includePaths)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        if (includePaths != null && includePaths.Length > 0)
        {
            ValidateIncludePaths(includePaths);
        }

        // Build base query (no includes) for selecting ids
        var baseQuery = asNoTracking ? dbSet.AsNoTracking().AsQueryable() : dbSet.AsQueryable();

        if (filter != null)
        {
            baseQuery = baseQuery.Where(filter);
        }

        // Apply ordering on baseQuery
        if (sortDescriptors != null && sortDescriptors.Any())
        {
            baseQuery = baseQuery.ApplyOrdering(sortDescriptors, allowedProperties);
        }
        else if (!string.IsNullOrWhiteSpace(orderByProperty))
        {
            if (allowedProperties != null)
            {
                var allowedSet = new HashSet<string>(allowedProperties, StringComparer.OrdinalIgnoreCase);
                if (!allowedSet.Contains(orderByProperty))
                    throw new ArgumentException($"Property '{orderByProperty}' is not allowed for ordering.");
            }

            baseQuery = baseQuery.ApplyOrderByProperty(orderByProperty, descending);
        }

        // Get primary key property name via EF metadata
        var entityType = dbContext.Model.FindEntityType(typeof(T)) ?? throw new InvalidOperationException($"Entity type {typeof(T).FullName} not found in EF model.");
        var pk = entityType.FindPrimaryKey() ?? throw new InvalidOperationException($"Primary key not found for {typeof(T).FullName}.");

        if (pk.Properties.Count != 1)
        {
            throw new NotSupportedException("Composite keys are not supported by GetPagedTwoStageAsync.");
        }

        var pkProperty = pk.Properties[0];
        var pkName = pkProperty.Name;

        // Build selector to project to PK value: e => EF.Property<TKey>(e, pkName)
        var parameter = Expression.Parameter(typeof(T), "e");
        var body = Expression.Call(typeof(EF), nameof(EF.Property), [typeof(TKey)], parameter, Expression.Constant(pkName));
        var selector = Expression.Lambda<Func<T, TKey>>(body, parameter);

        // Query ids with paging
        var skip = (pageNumber - 1) * pageSize;
        var idList = await baseQuery.Select(selector).Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

        // Total count
        var totalCount = await baseQuery.CountAsync(cancellationToken);

        if (idList.Count == 0)
            return new PagedResult<T>(Array.Empty<T>(), totalCount, pageNumber, pageSize);

        // Load entities with includes filtering by ids
        var loadQuery = asNoTracking ? dbSet.AsNoTracking().AsQueryable() : dbSet.AsQueryable();
        if (includePaths != null && includePaths.Length > 0)
        {
            loadQuery = loadQuery.IncludePaths(includePaths);
        }

        // Build contains expression: e => idList.Contains(EF.Property<TKey>(e, pkName))
        var containsBody = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [typeof(TKey)],
            Expression.Constant(idList),
            Expression.Call(typeof(EF), nameof(EF.Property), new[] { typeof(TKey) }, parameter, Expression.Constant(pkName)));
        var containsLambda = Expression.Lambda<Func<T, bool>>(containsBody, parameter);

        var loaded = await loadQuery.Where(containsLambda).ToListAsync(cancellationToken);

        // Reorder loaded entities to match idList order
        var propInfo = typeof(T).GetProperty(pkName) ?? throw new InvalidOperationException($"PK property {pkName} not found on type {typeof(T).Name}");
        var dict = loaded.ToDictionary(e => (TKey)propInfo.GetValue(e)!, e => e);
        var orderedItems = idList.Select(id => dict.ContainsKey(id) ? dict[id] : null).Where(x => x != null)!.Cast<T>().ToList();

        return new PagedResult<T>(orderedItems, totalCount, pageNumber, pageSize);
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
        => await dbSet.CountAsync(cancellationToken);

    public virtual async Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        return entity is not null;
    }

    public virtual async Task<PagedResult<T>> GetPagedTwoStageAsync(int pageNumber, int pageSize, IEnumerable<SortDescriptor>? sortDescriptors = null,
        string? orderByProperty = null, bool descending = false, Expression<Func<T, bool>>? filter = null, bool asNoTracking = true,
        CancellationToken cancellationToken = default, bool useAsSplitQuery = false, params string[] includePaths)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        if (includePaths != null && includePaths.Length > 0)
        {
            ValidateIncludePaths(includePaths);
        }

        var baseQuery = asNoTracking ? dbSet.AsNoTracking() : dbSet;

        if (filter != null)
        {
            baseQuery = baseQuery.Where(filter);
        }

        // Apply ordering on baseQuery
        if (sortDescriptors != null && sortDescriptors.Any())
        {
            baseQuery = baseQuery.ApplyOrdering(sortDescriptors, allowedProperties);
        }
        else if (!string.IsNullOrWhiteSpace(orderByProperty))
        {
            if (allowedProperties != null)
            {
                var allowedSet = new HashSet<string>(allowedProperties, StringComparer.OrdinalIgnoreCase);
                if (!allowedSet.Contains(orderByProperty))
                    throw new ArgumentException($"Property '{orderByProperty}' is not allowed for ordering.");
            }

            baseQuery = baseQuery.ApplyOrderByProperty(orderByProperty, descending);
        }

        // Get primary key property name via EF metadata
        var entityType = dbContext.Model.FindEntityType(typeof(T)) ?? throw new InvalidOperationException($"Entity type {typeof(T).FullName} not found in EF model.");
        var pk = entityType.FindPrimaryKey() ?? throw new InvalidOperationException($"Primary key not found for {typeof(T).FullName}.");
        if (pk.Properties.Count != 1)
        {
            throw new NotSupportedException("Composite keys are not supported by GetPagedTwoStageAsync.");
        }

        var pkProperty = pk.Properties[0];
        var pkName = pkProperty.Name;

        // Build selector to project to PK value: e => EF.Property<TKey>(e, pkName)
        var parameter = Expression.Parameter(typeof(T), "e");
        var body = Expression.Call(typeof(EF), nameof(EF.Property), [typeof(TKey)], parameter, Expression.Constant(pkName));
        var selector = Expression.Lambda<Func<T, TKey>>(body, parameter);

        // Query ids with paging
        var skip = (pageNumber - 1) * pageSize;
        var idList = await baseQuery.Select(selector).Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

        // Total count
        var totalCount = await baseQuery.CountAsync(cancellationToken);

        if (idList.Count == 0)
            return new PagedResult<T>([], totalCount, pageNumber, pageSize);

        // Load entities with includes filtering by ids
        var loadQuery = asNoTracking ? dbSet.AsNoTracking() : dbSet;

        if (includePaths != null && includePaths.Length > 0)
        {
            loadQuery = loadQuery.IncludePaths(includePaths);

            if (useAsSplitQuery)
            {
                loadQuery = loadQuery.AsSplitQuery(); // MEMO: AsSplitQuery is disponible in nuget package Microsoft.EntityFrameworkCore.Relational
            }
        }

        // Build contains expression: e => idList.Contains(EF.Property<TKey>(e, pkName))
        var containsBody = Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), [typeof(TKey)],
            Expression.Constant(idList), Expression.Call(typeof(EF), nameof(EF.Property), new[] { typeof(TKey) }, parameter, Expression.Constant(pkName)));

        var containsLambda = Expression.Lambda<Func<T, bool>>(containsBody, parameter);
        var loaded = await loadQuery.Where(containsLambda).ToListAsync(cancellationToken);

        // Reorder loaded entities to match idList order
        var propInfo = typeof(T).GetProperty(pkName)
            ?? throw new InvalidOperationException($"PK property {pkName} not found on type {typeof(T).Name}");

        var dict = loaded.ToDictionary(e => (TKey)propInfo.GetValue(e)!, e => e);
        var orderedItems = idList.Select(id => dict.ContainsKey(id) ? dict[id] : null).Where(x => x != null)!.Cast<T>().ToList();

        return new PagedResult<T>(orderedItems, totalCount, pageNumber, pageSize);
    }

    private void ValidateIncludePaths(string[] includePaths)
    {
        if (allowedIncludePaths != null && allowedIncludePaths.Any())
        {
            var allowed = new HashSet<string>(allowedIncludePaths, StringComparer.OrdinalIgnoreCase);
            foreach (var p in includePaths)
            {
                if (string.IsNullOrWhiteSpace(p))
                {
                    continue;
                }

                // exact or parent allowed (e.g. allowed "Author" allows "Author.Books")
                var segments = p.Split('.');
                var check = "";
                var ok = false;

                for (var i = 0; i < segments.Length; i++)
                {
                    check = i == 0 ? segments[i] : $"{check}.{segments[i]}";

                    if (!allowed.Contains(p))
                    {
                        throw new ArgumentException($"Include path '{p}' is not allowed.");
                    }

                    if (allowed.Contains(check))
                    {
                        ok = true;
                        break;
                    }
                }

                if (!ok)
                {
                    throw new ArgumentException($"Include path '{p}' is not allowed.");
                }
            }
        }
    }
}