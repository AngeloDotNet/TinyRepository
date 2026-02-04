using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TinyRepository.Interfaces;

namespace TinyRepository;

public class UnitOfWork<TContext>(TContext context) : IUnitOfWork where TContext : DbContext
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var tx = await context.Database.BeginTransactionAsync(cancellationToken);
        return new EfUnitOfWorkTransaction(tx);
    }

    public sealed class EfUnitOfWorkTransaction(IDbContextTransaction tx) : IUnitOfWorkTransaction
    {
        private bool disposed;

        public async Task CommitAsync(CancellationToken cancellationToken = default)
            => await tx.CommitAsync(cancellationToken);

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
            => await tx.RollbackAsync(cancellationToken);

        public async ValueTask DisposeAsync()
        {
            if (!disposed)
            {
                await tx.DisposeAsync();
                disposed = true;
            }
        }
    }
}