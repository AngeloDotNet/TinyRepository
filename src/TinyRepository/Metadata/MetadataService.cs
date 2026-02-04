using System.Reflection;
using TinyRepository.DTOs;
using TinyRepository.Extensions;
using TinyRepository.Metadata.Interfaces;

namespace TinyRepository.Metadata;

/// <summary>
/// Scanner-based metadata service. Scansiona gli assembly forniti per costruire
/// alias/orderable/include lists per tipo.
/// </summary>
public class MetadataService(params Assembly[] assemblies) : IMetadataService
{
    private readonly Assembly[] assemblies = (assemblies == null || assemblies.Length == 0) ? [Assembly.GetCallingAssembly()] : assemblies;

    public Task<EntityWhitelistDto?> GetEntityWhitelistAsync(string entityName)
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            return Task.FromResult<EntityWhitelistDto?>(null);
        }

        // find type by simple name or full name (case-insensitive)
        var type = assemblies.SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => string.Equals(t.Name, entityName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(t.FullName, entityName, StringComparison.OrdinalIgnoreCase));

        if (type == null)
        {
            return Task.FromResult<EntityWhitelistDto?>(null);
        }

        // build result using the scanners that already expose alias-aware info
        // For orderable: we use GetOrderablePropertiesWithAlias to get alias->actualPath pairs
        var orderablesWithAlias = OrderablePropertyScanner.GetOrderablePropertiesWithAlias(type, maxDepth: 5).ToArray();
        var includesWithAlias = IncludePathScanner.GetIncludePathsWithAlias(type, maxDepth: 5).ToArray();
        var aliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // orderable aliases: key=alias/effectiveName, value=actualPath
        foreach (var kv in orderablesWithAlias)
        {
            // kv.Key = alias/effectiveName, kv.Value = actualPath
            if (!aliasMap.ContainsKey(kv.Key))
            {
                aliasMap[kv.Key] = kv.Value;
            }
        }

        // include aliases
        foreach (var kv in includesWithAlias)
        {
            if (!aliasMap.ContainsKey(kv.Key))
            {
                aliasMap[kv.Key] = kv.Value;
            }
        }

        var dto = new EntityWhitelistDto
        {
            EntityType = type.FullName ?? type.Name,
            Aliases = aliasMap,
            OrderableProperties = orderablesWithAlias.Select(kv => kv.Value).Distinct(StringComparer.OrdinalIgnoreCase),
            IncludePaths = includesWithAlias.Select(kv => kv.Value).Distinct(StringComparer.OrdinalIgnoreCase)
        };

        return Task.FromResult<EntityWhitelistDto?>(dto);
    }
}