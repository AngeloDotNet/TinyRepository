using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TinyRepository.Interfaces;

namespace TinyRepository.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers generic repository and unit of work types.
    /// Note: register your DbContext (TContext) separately (e.g. AddDbContext<TContext>).
    /// </summary>
    public static IServiceCollection AddRepositoryPattern<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
        services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork<TContext>));

        // TContext must be registered by the caller (AddDbContext<TContext>)
        return services;
    }
}
