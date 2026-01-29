using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TinyRepository.Interfaces;
using TinyRepository.Options;
using TinyRepository.Provider;
using TinyRepository.Provider.Interfaces;
using TinyRepository.Sorting;

namespace TinyRepository.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositoryPattern<TContext>(this IServiceCollection services) where TContext : DbContext
    {
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());

        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
        services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork<TContext>));

        return services;
    }

    public static IServiceCollection AddAttributeWhitelistScan(this IServiceCollection services, Action<AttributeWhitelistScanOptions>? configure = null,
        params Assembly[]? assemblies)
    {
        var options = new AttributeWhitelistScanOptions();
        configure?.Invoke(options);

        var scanAssemblies = (assemblies == null || assemblies.Length == 0) ? [Assembly.GetCallingAssembly()] : assemblies.Distinct().ToArray();

        var types = scanAssemblies.SelectMany(a => a.GetTypes()).Where(t => t.IsClass && !t.IsAbstract).ToArray();

        foreach (var t in types)
        {
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var hasOrderable = props.Any(p => p.GetCustomAttribute(typeof(OrderableAttribute), inherit: true) != null);
            var hasInclude = props.Any(p => p.GetCustomAttribute(typeof(IncludeAllowedAttribute), inherit: true) != null);

            if (hasOrderable || hasInclude)
            {
                var providerType = typeof(AttributeWhitelistProvider<>).MakeGenericType(t);
                var ifaceProp = typeof(IPropertyWhitelistProvider<>).MakeGenericType(t);
                var ifaceInclude = typeof(IIncludeWhitelistProvider<>).MakeGenericType(t);

                // create single instance per type, registered as both interfaces
                var instance = Activator.CreateInstance(providerType, options.MaxDepth)!;

                services.AddSingleton(ifaceProp, _ => instance);
                services.AddSingleton(ifaceInclude, _ => instance);
            }
        }

        return services;
    }
}