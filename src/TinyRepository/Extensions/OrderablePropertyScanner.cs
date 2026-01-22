using System.Reflection;
using TinyRepository.Sorting;

namespace TinyRepository.Extensions;

/// <summary>
/// Scanner che costruisce una whitelist di proprietà ordinabili leggendo [Orderable]
/// e costruendo percorsi annidati (es. "Author.LastName") quando la proprietà finale è decorata.
/// Evita collezioni e tipi primitivi/value-type; limita la profondità per evitare ricorsioni infinite.
/// </summary>
public static class OrderablePropertyScanner
{
    private const int DefaultMaxDepth = 5;

    public static IEnumerable<string> GetOrderableProperties<T>() => GetOrderableProperties(typeof(T), DefaultMaxDepth);

    public static IEnumerable<string> GetOrderableProperties(Type type, int maxDepth = DefaultMaxDepth)
    {
        var results = new List<string>();
        var visited = new HashSet<Type>();
        BuildPaths(type, prefix: null, results, visited, 0, maxDepth);
        return results.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static void BuildPaths(Type type, string? prefix, List<string> results, HashSet<Type> visited, int depth, int maxDepth)
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
            // Skip indexers
            if (p.GetIndexParameters().Length > 0)
            {
                continue;
            }

            // Determine alias or name
            var orderableAttr = p.GetCustomAttribute<OrderableAttribute>(inherit: true);
            var propName = p.Name;
            var effectiveName = orderableAttr?.Alias ?? propName;

            // If the property itself is decorated -> include prefix.propName
            if (orderableAttr != null)
            {
                var path = string.IsNullOrEmpty(prefix) ? effectiveName : $"{prefix}.{effectiveName}";
                results.Add(path);
            }

            // If property is candidate complex type, try to recurse and add nested decorated properties
            var propertyType = p.PropertyType;
            propertyType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (IsCollectionType(propertyType) || IsPrimitiveOrKnownSimple(propertyType))
            {
                // skip recursion into collections and primitives/strings/value types
                continue;
            }

            // recursion into complex type
            var nestedPrefix = string.IsNullOrEmpty(prefix) ? propName : $"{prefix}.{propName}";
            BuildPaths(propertyType, nestedPrefix, results, visited, depth + 1, maxDepth);
        }

        visited.Remove(type);
    }

    private static bool IsCollectionType(Type type)
    {
        if (type == typeof(string))
        {
            return false;
        }

        return typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
    }

    private static bool IsPrimitiveOrKnownSimple(Type type)
    {
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime)
            || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid)
            || Nullable.GetUnderlyingType(type) != null && IsPrimitiveOrKnownSimple(Nullable.GetUnderlyingType(type));
    }
}