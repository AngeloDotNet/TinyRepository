using System.Collections.Concurrent;
using System.Reflection;
using TinyRepository.Sorting;

namespace TinyRepository.Extensions;

public static class IncludePathScanner
{
    private const int DefaultMaxDepth = 5;
    private static readonly ConcurrentDictionary<(Type, int), string[]> cache = new();

    public static IEnumerable<string> GetIncludePaths<T>() => GetIncludePaths(typeof(T), DefaultMaxDepth);

    public static IEnumerable<string> GetIncludePaths(Type type, int maxDepth = DefaultMaxDepth)
    {
        var key = (type, maxDepth);
        var arr = cache.GetOrAdd(key, _ => BuildPaths(type, maxDepth).ToArray());

        return arr;
    }

    public static IEnumerable<KeyValuePair<string, string>> GetIncludePathsWithAlias(Type type, int maxDepth = DefaultMaxDepth)
        => BuildPathsWithAlias(type, maxDepth);

    private static IEnumerable<string> BuildPaths(Type type, int maxDepth)
    {
        var results = new List<string>();
        var visited = new HashSet<Type>();

        BuildPathsRecursive(type, prefix: null, results, visited, 0, maxDepth);

        return results.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<KeyValuePair<string, string>> BuildPathsWithAlias(Type type, int maxDepth)
    {
        var results = new List<KeyValuePair<string, string>>();
        var visited = new HashSet<Type>();

        BuildPathsWithAliasRecursive(type, prefix: null, results, visited, 0, maxDepth);

        return results.GroupBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase).Select(g => g.First());
    }

    private static void BuildPathsRecursive(Type type, string? prefix, List<string> results, HashSet<Type> visited, int depth, int maxDepth)
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

            if (includeAttr != null)
            {
                var path = string.IsNullOrEmpty(prefix) ? effectiveName : $"{prefix}.{effectiveName}";
                results.Add(path);
            }

            var propertyType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

            // Dont recurse into collections
            if (IsCollectionType(propertyType) || IsPrimitiveOrKnownSimple(propertyType))
            {
                continue;
            }

            var nestedPrefix = string.IsNullOrEmpty(prefix) ? propName : $"{prefix}.{propName}";
            BuildPathsRecursive(propertyType, nestedPrefix, results, visited, depth + 1, maxDepth);
        }

        visited.Remove(type);
    }

    private static void BuildPathsWithAliasRecursive(Type type, string? prefix, List<KeyValuePair<string, string>> results,
        HashSet<Type> visited, int depth, int maxDepth)
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

            if (includeAttr != null)
            {
                var path = string.IsNullOrEmpty(prefix) ? effectiveName : $"{prefix}.{effectiveName}";
                var actualPath = string.IsNullOrEmpty(prefix) ? propName : $"{prefix}.{propName}";
                results.Add(new KeyValuePair<string, string>(effectiveName, actualPath));
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