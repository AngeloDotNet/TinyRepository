namespace TinyRepository.Provider;

/// <summary>
/// Implementa questa interfaccia e registrala in DI per fornire una whitelist di proprietà
/// utilizzabili per l'ordinamento su una specifica entità T.
/// Esempio: builder.Services.AddSingleton<IPropertyWhitelistProvider<Article>, ArticleWhitelist>();
/// </summary>
public interface IPropertyWhitelistProvider<T>
{
    IEnumerable<string> GetAllowedProperties();
}