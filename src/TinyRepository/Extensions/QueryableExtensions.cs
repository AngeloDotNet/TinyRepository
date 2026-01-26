using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TinyRepository.Enums;
using TinyRepository.Sorting;

namespace TinyRepository.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> IncludeMultiple<T>(this IQueryable<T> query, params Expression<Func<T, object>>[] includes) where T : class
    {
        if (includes == null || includes.Length == 0)
        {
            return query;
        }

        foreach (var include in includes)
        {
            if (include != null)
                query = query.Include(include);
        }

        return query;
    }

    public static IQueryable<T> IncludePaths<T>(this IQueryable<T> query, params string[] includePaths) where T : class
    {
        if (includePaths == null || includePaths.Length == 0)
        {
            return query;
        }

        // Rimuoviamo duplicati e ordiniamo in modo che i percorsi "padre" vengano prima dei figli.
        // Questo non è strettamente necessario per EF Core, ma aiuta in scenari di costruzione di query leggibili.
        var normalized = NormalizeAndOrderPaths(includePaths);

        foreach (var path in normalized)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                query = query.Include(path);
            }
        }

        return query;
    }

    public static IQueryable<T> ApplyOrderByProperty<T>(this IQueryable<T> source, string? orderByProperty, bool descending = false)
    {
        if (string.IsNullOrWhiteSpace(orderByProperty))
        {
            return source;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? body = parameter;

        foreach (var member in orderByProperty.Split('.'))
        {
            body = Expression.PropertyOrField(body!, member);
        }

        if (body == null)
        {
            return source;
        }

        var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), body.Type);
        var lambda = Expression.Lambda(delegateType, body, parameter);

        var methodName = descending ? "OrderByDescending" : "OrderBy";
        var queryableType = typeof(Queryable);

        var orderingMethod = queryableType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == 2);

        if (orderingMethod == null)
        {
            return source;
        }

        var genericMethod = orderingMethod.MakeGenericMethod(typeof(T), body.Type);
        var ordered = (IQueryable<T>)genericMethod.Invoke(null, [source, lambda])!;

        return ordered;
    }

    public static IQueryable<T> ApplyOrdering<T>(this IQueryable<T> source, IEnumerable<SortDescriptor> sortDescriptors,
        IEnumerable<string>? allowedProperties = null)
    {
        if (sortDescriptors == null)
        {
            return source;
        }

        var descriptors = sortDescriptors.ToArray();

        if (descriptors.Length == 0)
        {
            return source;
        }

        // VALIDAZIONE WHITELIST (se fornita)
        if (allowedProperties != null)
        {
            var allowedSet = new HashSet<string>(allowedProperties, StringComparer.OrdinalIgnoreCase);

            foreach (var d in descriptors)
            {
                if (!allowedSet.Contains(d.PropertyName))
                {
                    throw new ArgumentException($"Property '{d.PropertyName}' is not allowed for ordering.");
                }
            }
        }

        IOrderedQueryable<T>? orderedQuery = null;
        var parameter = Expression.Parameter(typeof(T), "x");

        for (var i = 0; i < descriptors.Length; i++)
        {
            var desc = descriptors[i];

            // Build body expression for nested properties
            Expression? body = parameter;

            foreach (var member in desc.PropertyName.Split('.'))
            {
                body = Expression.PropertyOrField(body!, member);
            }

            if (body == null)
            {
                continue;
            }

            var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), body.Type);
            var lambda = Expression.Lambda(delegateType, body, parameter);

            string methodName;

            if (i == 0)
            {
                methodName = desc.Direction == SortDirection.Descending ? "OrderByDescending" : "OrderBy";
            }
            else
            {
                methodName = desc.Direction == SortDirection.Descending ? "ThenByDescending" : "ThenBy";
            }

            var queryableType = typeof(Queryable);
            var methods = queryableType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == methodName && m.GetParameters().Length == 2).ToList();

            var method = methods.FirstOrDefault();

            if (method == null)
            {
                continue;
            }

            var genericMethod = method.MakeGenericMethod(typeof(T), body.Type);

            if (i == 0)
            {
                orderedQuery = (IOrderedQueryable<T>)genericMethod.Invoke(null, new object[] { source, lambda })!;
            }
            else
            {
                orderedQuery = (IOrderedQueryable<T>)genericMethod.Invoke(null, new object[] { orderedQuery!, lambda })!;
            }
        }

        return orderedQuery ?? source;
    }

    private static string[] NormalizeAndOrderPaths(string[] includePaths)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in includePaths)
        {
            if (string.IsNullOrWhiteSpace(p))
            {
                continue;
            }

            var trimmed = p.Trim();
            set.Add(trimmed);
        }

        // Order by number of segments ascending: parents before children
        return set.OrderBy(p => p.Count(c => c == '.')).ThenBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}