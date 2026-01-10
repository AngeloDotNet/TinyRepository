# RepositoryPattern (generic, EF Core, .NET 8)

Questa libreria fornisce un repository generico basato su EF Core con i principali metodi CRUD (asincroni).

Principali componenti:
- `IEntity<TKey>`: interfaccia base per le entità.
- `IRepository<T, TKey>`: interfaccia repository generica.
- `EfRepository<T, TKey>`: implementazione basata su `DbContext`.
- `IUnitOfWork` / `UnitOfWork<TContext>`: per SaveChanges.
- Estensioni per registrazione DI.

Esempio di registrazione in ASP.NET Core:
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork<AppDbContext>>();
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

Note:
- L'implementazione non chiama SaveChanges internamente su Add/Update/Remove: il commit è responsabilità dell'unit-of-work.
- Puoi estendere `EfRepository` aggiungendo metodi per paging, projection, includi (Include), ecc.
- Se preferisci metodi sincroni, puoi aggiungerli ma in app moderne è preferibile usare gli async.