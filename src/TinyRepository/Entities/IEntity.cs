namespace TinyRepository.Entities;

/// <summary>
/// Represents a domain entity that exposes a single identifying key.
/// </summary>
/// <typeparam name="TKey">
/// The type used for the entity identifier. This type must implement <see cref="System.IEquatable{TKey}"/>
/// so that identifiers can be compared for equality in a reliable and type-safe manner.
/// </typeparam>
/// <remarks>
/// Use this interface for simple entities that are identified by a single key property named <c>Id</c>.
/// Keeping entities typed by their key improves clarity and enables generic repository implementations
/// to operate on entities with different identifier types (for example, <c>int</c>, <c>Guid</c>, or <c>string</c>).
/// 
/// Implementations should ensure that <c>Id</c> represents a stable identity for the lifetime of the entity
/// and that equality semantics of <typeparamref name="TKey"/> are appropriate for identity comparisons.
/// </remarks>
/// <example>
/// A typical implementation using an integer key:
/// <code language="csharp">
/// public class User : IEntity&lt;int&gt;
/// {
///     public int Id { get; set; }
///     public string Name { get; set; } = string.Empty;
/// }
/// </code>
/// </example>
public interface IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    /// <value>
    /// The entity identifier of type <typeparamref name="TKey"/>. The identifier should uniquely
    /// identify the entity instance within its aggregate or repository scope.
    /// </value>
    TKey Id { get; set; }
}