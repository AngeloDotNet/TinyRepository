using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

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
    /// Applica ordering dinamico per nome proprietà (supporta percorsi con dot, es. "Author.Name").
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
}