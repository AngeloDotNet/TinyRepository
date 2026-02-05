using System.Reflection;
using TinyRepository.DTOs;
using TinyRepository.Extensions;
using TinyRepository.Metadata.Interfaces;

namespace TinyRepository.Metadata;

public class MetadataService(int maxDepth = 5, params Assembly[] assemblies) : IMetadataService
{
    private readonly Assembly[] assemblies = (assemblies == null || assemblies.Length == 0) ? [Assembly.GetCallingAssembly()] : assemblies;
    private readonly int maxDepth = maxDepth <= 0 ? 5 : maxDepth;
    private readonly Lazy<Dictionary<string, Type>> typesBySimpleName = new Lazy<Dictionary<string, Type>>(() =>
    {
        return assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);
    });

    public Task<EntityWhitelistDto?> GetEntityWhitelistAsync(string entityName)
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            return Task.FromResult<EntityWhitelistDto?>(null);
        }

        var type = FindTypeByName(entityName);
        if (type == null)
        {
            return Task.FromResult<EntityWhitelistDto?>(null);
        }

        var orderables = OrderablePropertyScanner.GetOrderablePropertiesWithAlias(type, maxDepth).ToArray();
        var includes = IncludePathScanner.GetIncludePathsWithAlias(type, maxDepth).ToArray();

        var aliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var aliasInfos = new List<AliasMetadata>();

        foreach (var a in orderables)
        {
            if (!aliasMap.ContainsKey(a.Alias))
            {
                aliasMap[a.Alias] = a.Path;
            }

            aliasInfos.Add(a);
        }

        foreach (var a in includes)
        {
            if (!aliasMap.ContainsKey(a.Alias))
            {
                aliasMap[a.Alias] = a.Path;
            }

            aliasInfos.Add(a);
        }

        var dto = new EntityWhitelistDto
        {
            EntityType = type.FullName ?? type.Name,
            Aliases = aliasMap,
            AliasInfos = aliasInfos.GroupBy(x => x.Alias, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToArray(),
            OrderableProperties = orderables.Select(x => x.Path).Distinct(StringComparer.OrdinalIgnoreCase),
            IncludePaths = includes.Select(x => x.Path).Distinct(StringComparer.OrdinalIgnoreCase)
        };

        return Task.FromResult<EntityWhitelistDto?>(dto);
    }

    public Task<IEnumerable<string>> GetAllEntityNamesAsync()
    {
        var names = typesBySimpleName.Value.Keys.OrderBy(n => n).AsEnumerable();
        return Task.FromResult(names);
    }

    private Type? FindTypeByName(string entityName)
    {
        if (typesBySimpleName.Value.TryGetValue(entityName, out var t)) return t;
        // try full name
        var full = assemblies.SelectMany(a => a.GetTypes()).FirstOrDefault(x => string.Equals(x.FullName, entityName, StringComparison.OrdinalIgnoreCase));
        return full;
    }
}