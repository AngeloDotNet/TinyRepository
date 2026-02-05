using TinyRepository.Extensions;
using TinyRepository.Provider.Interfaces;

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
        orderables = OrderablePropertyScanner.GetOrderablePropertiesWithAlias(typeof(T), maxDepth).Select(a => a.Path).ToArray();
        includes = IncludePathScanner.GetIncludePathsWithAlias(typeof(T), maxDepth).Select(a => a.Path).ToArray();
        aliasMap = AttributeWhitelistProvider<T>.BuildAliasMap(typeof(T), this.maxDepth);
    }

    public IEnumerable<string> GetAllowedProperties() => orderables;
    public IEnumerable<string> GetAllowedIncludePaths() => includes;
    public IDictionary<string, string> GetAliasMap() => aliasMap;

    private static IDictionary<string, string> BuildAliasMap(Type type, int maxDepth)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var orderablesWithAliases = OrderablePropertyScanner.GetOrderablePropertiesWithAlias(type, maxDepth);

        foreach (var aliasMetadata in orderablesWithAliases)
        {
            // aliasMetadata.Alias = alias, aliasMetadata.Path = actualPath
            if (aliasMetadata.Alias is not null && !map.ContainsKey(aliasMetadata.Alias))
            {
                map[aliasMetadata.Alias] = aliasMetadata.Path;
            }
        }

        var includesWithAliases = IncludePathScanner.GetIncludePathsWithAlias(type, maxDepth);

        foreach (var aliasMetadata in includesWithAliases)
        {
            if (aliasMetadata.Alias is not null && !map.ContainsKey(aliasMetadata.Alias))
            {
                map[aliasMetadata.Alias] = aliasMetadata.Path;
            }
        }

        return map;
    }
}