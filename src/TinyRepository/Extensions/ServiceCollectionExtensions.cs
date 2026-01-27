using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TinyRepository.Interfaces;
using TinyRepository.Provider.Interfaces;
using TinyRepository.Sorting;
using TinyRepository.Sorting.Provider;

namespace TinyRepository.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositoryPattern<TContext>(this IServiceCollection services) where TContext : DbContext
    {
        //services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
        //services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork<TContext>));

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());

        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
        services.AddScoped<IUnitOfWork>(sp => new UnitOfWork<TContext>(sp.GetRequiredService<TContext>()));

        // Register UnitOfWork tied to the same DbContext type:
        services.AddScoped<UnitOfWork<TContext>>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UnitOfWork<TContext>>());

        return services;
    }

    public static IServiceCollection AddAttributeWhitelistScan(this IServiceCollection services, params Assembly[]? assemblies)
    {
        var scanAssemblies = (assemblies == null || assemblies.Length == 0) ? [Assembly.GetCallingAssembly()] : assemblies.Distinct().ToArray();
        var types = scanAssemblies.SelectMany(a => a.GetTypes()).Where(t => t.IsClass && !t.IsAbstract).ToArray();

        foreach (var t in types)
        {
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var hasOrderable = props.Any(p => p.GetCustomAttribute(typeof(OrderableAttribute), inherit: true) != null);
            var hasInclude = props.Any(p => p.GetCustomAttribute(typeof(IncludeAllowedAttribute), inherit: true) != null);

            if (hasOrderable || hasInclude)
            {
                // register AttributeWhitelistProvider<T> as singleton for both interfaces
                var providerType = typeof(AttributeWhitelistProvider<>).MakeGenericType(t);
                var ifaceProp = typeof(IPropertyWhitelistProvider<>).MakeGenericType(t);
                var ifaceInclude = typeof(IIncludeWhitelistProvider<>).MakeGenericType(t);

                services.AddSingleton(ifaceProp, providerType);
                services.AddSingleton(ifaceInclude, providerType);
            }
        }

        return services;
    }
}
