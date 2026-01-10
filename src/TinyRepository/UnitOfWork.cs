using Microsoft.EntityFrameworkCore;
using TinyRepository.Interfaces;

namespace TinyRepository;

public class UnitOfWork<TContext>(TContext context) : IUnitOfWork where TContext : DbContext
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);
}