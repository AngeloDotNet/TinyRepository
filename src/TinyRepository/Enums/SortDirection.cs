namespace TinyRepository.Enums;

/// <summary>
/// Specifies the direction to use when sorting a sequence.
/// </summary>
/// <remarks>
/// Use <see cref="SortDirection.Ascending"/> to order values from smallest to largest,
/// and <see cref="SortDirection.Descending"/> to order values from largest to smallest.
/// This enum is intended for repository or query APIs that accept a sort direction parameter,
/// and is commonly used together with LINQ's <c>OrderBy</c> / <c>OrderByDescending</c> methods.
/// </remarks>
/// <example>
/// <code>
/// // Example: choose LINQ ordering based on the sort direction
/// var result = sortDirection == SortDirection.Ascending
///     ? items.OrderBy(x => x.Name)
///     : items.OrderByDescending(x => x.Name);
/// </code>
/// </example>
public enum SortDirection
{
    /// <summary>
    /// Sort in ascending order (from smallest to largest).
    /// </summary>
    Ascending = 0,

    /// <summary>
    /// Sort in descending order (from largest to smallest).
    /// </summary>
    Descending = 1
}