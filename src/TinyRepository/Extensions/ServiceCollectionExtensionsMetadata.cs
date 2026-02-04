using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TinyRepository.Metadata;
using TinyRepository.Metadata.Interfaces;

namespace TinyRepository.Extensions;

public static class ServiceCollectionExtensionsMetadata
{
    /// <summary>
    /// Registra il MetadataService con gli assembly forniti.
    /// </summary>
    public static IServiceCollection AddMetadataService(this IServiceCollection services, params Assembly[] assemblies)
        => services.AddSingleton<IMetadataService>(new MetadataService(assemblies));
}