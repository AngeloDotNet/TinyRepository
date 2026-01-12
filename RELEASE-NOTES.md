### 💡 Release Notes

v.1.0.1

- Enhancement: `IEntity<TKey>`: Basic interface for entities.
- Enhancement: `IRepository<T, TKey>`: Generic repository interface.
- Enhancement: `EfRepository<T, TKey>`: Implementation based on `DbContext`.
- Enhancement: `IUnitOfWork` / `UnitOfWork<TContext>`: for SaveChanges.
- Enhancement: Extensions for DI registration.

v.1.0.5

- Enhancement: Paging (GetPagedAsync) with pageNumber/pageSize, optional filtering, and ordering via Func<IQueryable<T>, IOrderedQueryable<T>>.
- Enhancement: Expose IQueryable<T> via Query(asNoTracking) to build complex caller-side queries.
- Enhancement: PatchAsync(id, Action<T>) method to apply incremental changes to an entity.
- Enhancement: UpdateRangeAsync and RemoveRangeAsync (with async signature, in-memory operations on change tracker).
- Enhancement: PagedResult<T> as paging result (Items, TotalCount, PageNumber, PageSize).

v.1.0.8

- Enhancement: Dynamic ordering helper: You can pass the property name as a string (e.g., "Name" or "Author.Name") to GetPagedAsync.
- Enhancement: Query overload with params Expression<Func<T, object>>[] includes to conveniently include navigation properties.
- Enhancement: GetPagedAsync overload that accepts multiple includes in addition to dynamic ordering.