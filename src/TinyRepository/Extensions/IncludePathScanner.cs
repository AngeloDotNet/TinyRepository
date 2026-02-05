using System.Collections.Concurrent;
using System.Reflection;
using TinyRepository.Metadata;
using TinyRepository.Sorting;

namespace TinyRepository.Extensions;

public static class IncludePathScanner
{
    private const int DefaultMaxDepth = 5;
    private static readonly ConcurrentDictionary<(Type, int), AliasMetadata[]> cache = new();

    public static IEnumerable<AliasMetadata> GetIncludePathsWithAlias<T>(int maxDepth = DefaultMaxDepth)
        => GetIncludePathsWithAlias(typeof(T), maxDepth);

    public static IEnumerable<AliasMetadata> GetIncludePathsWithAlias(Type type, int maxDepth = DefaultMaxDepth)
    {
        var key = (type, maxDepth);
        var arr = cache.GetOrAdd(key, _ => BuildPathsWithAlias(type, maxDepth).ToArray());

        return arr;
    }

    private static IEnumerable<AliasMetadata> BuildPathsWithAlias(Type type, int maxDepth)
    {
        var results = new List<AliasMetadata>();
        var visited = new HashSet<Type>();
        BuildPathsWithAliasRecursive(type, prefix: null, results, visited, 0, maxDepth);

        return results.GroupBy(a => a.Alias, StringComparer.OrdinalIgnoreCase).Select(g => g.First());
    }

    private static void BuildPathsWithAliasRecursive(Type type, string? prefix, List<AliasMetadata> results, HashSet<Type> visited,
        int depth, int maxDepth)
    {
        if (type == null)
        {
            return;
        }

        if (depth > maxDepth)
        {
            return;
        }

        if (visited.Contains(type))
        {
            return;
        }

        visited.Add(type);

        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var p in props)
        {
            if (p.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var includeAttr = p.GetCustomAttribute<IncludeAllowedAttribute>(inherit: true);
            var propName = p.Name;
            var effectiveName = includeAttr?.Alias ?? propName;
            var desc = includeAttr?.Description;
            var example = includeAttr?.Example;

            if (includeAttr != null)
            {
                var actualPath = string.IsNullOrEmpty(prefix) ? propName : $"{prefix}.{propName}";
                var aliasPath = string.IsNullOrEmpty(prefix) ? effectiveName : $"{prefix}.{effectiveName}";

                if (string.IsNullOrWhiteSpace(desc))
                {
                    desc = XmlCommentsReader.GetPropertySummary(p.DeclaringType!, p.Name);
                }

                results.Add(new AliasMetadata
                {
                    Alias = aliasPath,
                    Path = actualPath,
                    Description = desc,
                    Example = example
                });
            }

            var propertyType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

            if (IsCollectionType(propertyType) || IsPrimitiveOrKnownSimple(propertyType))
            {
                continue;
            }

            var nestedPrefix = string.IsNullOrEmpty(prefix) ? propName : $"{prefix}.{propName}";
            BuildPathsWithAliasRecursive(propertyType, nestedPrefix, results, visited, depth + 1, maxDepth);
        }

        visited.Remove(type);
    }

    private static bool IsCollectionType(Type type)
    {
        if (type == typeof(string))
        {
            return false;
        }

        if (type.IsArray)
        {
            return true;
        }

        return typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string);
    }

    private static bool IsPrimitiveOrKnownSimple(Type type)
    {
        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || (Nullable.GetUnderlyingType(type) != null && IsPrimitiveOrKnownSimple(Nullable.GetUnderlyingType(type)));
    }
}