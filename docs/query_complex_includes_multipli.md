#Esempio: query complessa con includes multipli:

```csharp
var query = _repo.Query(asNoTracking: true, e => e.Author, e => e.Comments);
var list = await query
    .Where(e => e.CreatedAt > DateTime.UtcNow.AddDays(-7))
    .OrderBy(e => e.CreatedAt)
    .ToListAsync();
```