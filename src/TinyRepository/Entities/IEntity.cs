namespace TinyRepository.Entities;

public interface IEntity<TKey> where TKey : IEquatable<TKey>
{
    TKey Id { get; set; }
}