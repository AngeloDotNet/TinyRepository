using TinyRepository.Extensions;
using TinyRepository.Provider.Interfaces;

namespace TinyRepository.Provider;

/// <summary>
/// Provider che costruisce la whitelist leggendo gli attributi [Orderable] e [IncludeAllowed],
/// con profondità massima configurabile.
/// </summary>
public class AttributeWhitelistProvider<T>(int maxDepth = 5) : IPropertyWhitelistProvider<T>, IIncludeWhitelistProvider<T>
{
    public IEnumerable<string> GetAllowedProperties()
        => OrderablePropertyScanner.GetOrderableProperties(typeof(T), maxDepth);

    public IEnumerable<string> GetAllowedIncludePaths()
        => IncludePathScanner.GetIncludePaths(typeof(T), maxDepth);
}