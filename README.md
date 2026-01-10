# RepositoryPattern (generic, EF Core, .NET 8)

Questa libreria fornisce un repository generico basato su EF Core con i principali metodi CRUD (asincroni).

Principali componenti:
- `IEntity<TKey>`: interfaccia base per le entità.
- `IRepository<T, TKey>`: interfaccia repository generica.
- `EfRepository<T, TKey>`: implementazione basata su `DbContext`.
- `IUnitOfWork` / `UnitOfWork<TContext>`: per SaveChanges.
- Estensioni per registrazione DI.
- Paging (GetPagedAsync) con pageNumber/pageSize, filtro opzionale e ordering tramite Func<IQueryable<T>, IOrderedQueryable<T>>.
- Esposizione di IQueryable<T> tramite Query(asNoTracking) per costruire query complesse lato chiamante.
- Metodo PatchAsync(id, Action<T>) per applicare modifiche incrementali a un'entità.
- UpdateRangeAsync e RemoveRangeAsync (con firma async, operazioni in-memory su change tracker).
- PagedResult<T> come risultato del paging (Items, TotalCount, PageNumber, PageSize).

Esempio di registrazione DI (ASP.NET Core):
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddRepositoryPattern<AppDbContext>();
```

Uso in un service:
```csharp
public class MyService
{
    private readonly IRepository<SampleEntity, int> _repo;
    private readonly IUnitOfWork _uow;

    public MyService(IRepository<SampleEntity, int> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task CreateAsync(string name)
    {
        var entity = new SampleEntity { Name = name, CreatedAt = DateTime.UtcNow };
        await _repo.AddAsync(entity);
        await _uow.SaveChangesAsync();
    }
}
```

Esempio: paging con ordinamento
```csharp
// repository iniettato: IRepository<SampleEntity,int> _repo
var page = 1;
var pageSize = 10;

var paged = await _repo.GetPagedAsync(
    page,
    pageSize,
    orderBy: q => q.OrderBy(e => e.Name),   // oppure q => q.OrderByDescending(...)
    filter: e => e.Name != null && e.Name.Contains("abc"),
    asNoTracking: true);
```

Esempio: query complessa tramite IQueryable
```csharp
var queryable = _repo.Query(asNoTracking: false); // con tracking se vuoi modificare dopo
var special = await queryable
    .Where(e => e.CreatedAt > DateTime.UtcNow.AddDays(-30))
    .Include(e => EF.Property<object>(e, "Related")) // esempio Include dinamico
    .OrderBy(e => e.CreatedAt)
    .ToListAsync();
```

Esempio: PatchAsync
```csharp
var success = await _repo.PatchAsync(5, entity => {
    entity.Name = "Nuovo nome";
});
if (success) await uow.SaveChangesAsync();
```

<!--
Note:
- L'implementazione non chiama SaveChanges internamente su Add/Update/Remove: il commit è responsabilità dell'unit-of-work.
- Puoi estendere `EfRepository` aggiungendo metodi per paging, projection, includi (Include), ecc.
- Se preferisci metodi sincroni, puoi aggiungerli ma in app moderne è preferibile usare gli async.
- Le operazioni Add/Update/Remove/Range non chiamano SaveChanges internamente; è responsabilità dell'UnitOfWork/Service chiamante effettuare il commit.
- GetPagedAsync senza orderBy non garantisce ordine deterministico; è consigliato passare un orderBy per paginazione affidabile.
-->