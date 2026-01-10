using System.Linq.Expressions;
using TinyRepository.Entities;

namespace TinyRepository.Interfaces;

public interface IRepository<T, TKey>
    where T : class, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    void Update(T entity);
    void Remove(T entity);
    Task RemoveByIdAsync(TKey id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}