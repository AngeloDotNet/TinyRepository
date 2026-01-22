using TinyRepository.Extensions;
using TinyRepository.Provider;

namespace TinyRepository.Sorting.Provider;

/// <summary>
/// Provider opzionale che costruisce la whitelist leggendo gli attributi [Orderable].
/// Puoi registrarlo in DI se vuoi esporre esplicitamente una IPropertyWhitelistProvider<T>.
/// </summary>
public class AttributeWhitelistProvider<T> : IPropertyWhitelistProvider<T>
{
    public IEnumerable<string> GetAllowedProperties()
    {
        return OrderablePropertyScanner.GetOrderableProperties<T>();
    }
}
