using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TinyRepository.Interfaces;

namespace TinyRepository.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositoryPattern<TContext>(this IServiceCollection services) where TContext : DbContext, IUnitOfWork
    {
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));

        return services;
    }
}