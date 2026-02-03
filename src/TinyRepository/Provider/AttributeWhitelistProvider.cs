using TinyRepository.Extensions;
using TinyRepository.Provider.Interfaces;
using TinyRepository.Sorting.Interfaces;

namespace TinyRepository.Provider;

/// <summary>
/// Provider che costruisce la whitelist leggendo gli attributi [Orderable] e [IncludeAllowed],
/// con profondità massima configurabile. Espone anche la mappa alias->path.
/// </summary>
public class AttributeWhitelistProvider<T> : IPropertyWhitelistProvider<T>, IIncludeWhitelistProvider<T>, IAliasProvider<T>
{
    private readonly int maxDepth;
    private readonly IEnumerable<string> orderables;
    private readonly IEnumerable<string> includes;
    private readonly IDictionary<string, string> aliasMap;

    public AttributeWhitelistProvider(int maxDepth = 5)
    {
        this.maxDepth = maxDepth;
        orderables = OrderablePropertyScanner.GetOrderableProperties(typeof(T), maxDepth).ToArray();
        includes = IncludePathScanner.GetIncludePaths(typeof(T), maxDepth).ToArray();
        aliasMap = AttributeWhitelistProvider<T>.BuildAliasMap(typeof(T), this.maxDepth);
    }

    public IEnumerable<string> GetAllowedProperties() => orderables;
    public IEnumerable<string> GetAllowedIncludePaths() => includes;
    public IDictionary<string, string> GetAliasMap() => aliasMap;

    private static IDictionary<string, string> BuildAliasMap(Type type, int maxDepth)
    {
        // Reuse the scanners to gather alias info
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Orderable aliases
        var orderablesWithAliases = OrderablePropertyScanner.GetOrderablePropertiesWithAlias(type, maxDepth);
        foreach (var kv in orderablesWithAliases)
        {
            // kv.Key = alias or property name, kv.Value = actualPath
            if (!map.ContainsKey(kv.Key))
            {
                map[kv.Key] = kv.Value;
            }
        }

        var includesWithAliases = IncludePathScanner.GetIncludePathsWithAlias(type, maxDepth);
        foreach (var kv in includesWithAliases)
        {
            if (!map.ContainsKey(kv.Key))
            {
                map[kv.Key] = kv.Value;
            }
        }

        return map;
    }
}
