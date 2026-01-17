Esempio: Registrare una whitelist per un'entità

```csharp
// esempio per Article: consentire solo "Title" e "PublishedAt" come proprietà ordinabili
public class ArticleWhitelist : IPropertyWhitelistProvider<Article>
{
    public IEnumerable<string> GetAllowedProperties() => new[] { "Title", "PublishedAt" };
}

// in Program.cs / Startup.cs
builder.Services.AddSingleton<IPropertyWhitelistProvider<Article>, ArticleWhitelist>();


NOTA: Se vuoi una whitelist centralizzata per più entità, implementa IPropertyWhitelistProvider<T> per ogni T che ti interessa e registrala in DI.