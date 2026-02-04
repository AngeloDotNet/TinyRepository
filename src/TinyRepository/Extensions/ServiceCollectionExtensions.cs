using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TinyRepository.Interfaces;
using TinyRepository.Metadata;
using TinyRepository.Metadata.Interfaces;
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

    public static IServiceCollection AddAttributeWhitelistScan(this IServiceCollection services,
        Action<AttributeWhitelistScanOptions>? configure = null, params Assembly[]? assemblies)
    {
        var options = new AttributeWhitelistScanOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);

        var scanAssemblies = (assemblies == null || assemblies.Length == 0)
            ? [Assembly.GetCallingAssembly()] : assemblies.Distinct().ToArray();

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
                var ifaceAlias = typeof(IAliasProvider<>).MakeGenericType(t);

                var instance = Activator.CreateInstance(providerType, options.MaxDepth)!;

                services.AddSingleton(ifaceProp, _ => instance);
                services.AddSingleton(ifaceInclude, _ => instance);
                services.AddSingleton(ifaceAlias, _ => instance);
            }
        }

        return services;
    }

    public static IServiceCollection AddMetadataService(this IServiceCollection services, Action<MetadataServiceOptions>? configure = null, params Assembly[]? assemblies)
    {
        var opts = new MetadataServiceOptions();
        configure?.Invoke(opts);

        // If MaxDepth not set explicitly, try to reuse AttributeWhitelistScanOptions from DI (when AddAttributeWhitelistScan was called)
        // We'll register MetadataService as a factory so it can read AttributeWhitelistScanOptions from service provider.
        services.AddSingleton<IMetadataService>(sp =>
        {
            var maxDepth = opts.MaxDepth ?? sp.GetService<AttributeWhitelistScanOptions>()?.MaxDepth ?? 5;
            var scanAssemblies = (assemblies == null || assemblies.Length == 0)
                ? [Assembly.GetCallingAssembly()] : assemblies.Distinct().ToArray();

            return new MetadataService(maxDepth, scanAssemblies);
        });

        return services;
    }
}