#Esempio: query complessa tramite IQueryable

```csharp
var queryable = _repo.Query(asNoTracking: false); // con tracking se vuoi modificare dopo
var special = await queryable
    .Where(e => e.CreatedAt > DateTime.UtcNow.AddDays(-30))
    .Include(e => EF.Property<object>(e, "Related")) // esempio Include dinamico
    .OrderBy(e => e.CreatedAt)
    .ToListAsync();
```