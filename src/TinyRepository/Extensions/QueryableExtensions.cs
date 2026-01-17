using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TinyRepository.Enums;
using TinyRepository.Sorting;

namespace TinyRepository.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Applica Include multipli.
    /// </summary>
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

    /// <summary>
    /// Applica ordering dinamico per singola proprietà (supporta percorsi con dot, es. "Author.Name").
    /// Se orderByProperty è null/empty restituisce la query originale.
    /// </summary>
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

    /// <summary>
    /// Applica multiple ordinamenti (OrderBy / ThenBy) in base ai SortDescriptor.
    /// Se allowedProperties è non-null e non vuota, ogni property deve far parte della whitelist o viene lanciata un'eccezione.
    /// Supporta proprietà annidate con dot path ("Author.LastName").
    /// </summary>
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
                .Where(m => m.Name == methodName && m.GetParameters().Length == 2)
                .ToList();

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
}