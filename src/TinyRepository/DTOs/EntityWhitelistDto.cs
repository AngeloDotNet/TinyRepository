using TinyRepository.Metadata;

namespace TinyRepository.DTOs;

/// <summary>
/// Describes the set of allowed properties and aliases for a specific entity type
/// that the repository exposes for reads, ordering and include-path expansion.
/// </summary>
/// <remarks>
/// Use this DTO to convey a server-side whitelist for clients or for runtime
/// components that must validate or translate client-provided property names
/// and include paths. It contains both a simple alias map for backward
/// compatibility and a richer collection of <see cref="AliasMetadata"/> items
/// with description and example usage.
/// </remarks>
public class EntityWhitelistDto
{
    /// <summary>
    /// The logical or CLR type name of the entity this whitelist applies to.
    /// </summary>
    /// <value>
    /// For example: <c>"Book"</c> or the full CLR name such as
    /// <c>"MyApp.Domain.Models.Book"</c>, depending on how the repository
    /// exposes entity types to clients.
    /// </value>
    public string EntityType { get; init; } = string.Empty;

    /// <summary>
    /// Simple alias-to-actual-path map kept for backward compatibility.
    /// </summary>
    /// <remarks>
    /// Each entry maps an alias (key) to the actual navigational/property path
    /// understood by the repository (value). Example:
    /// <code>
    /// { "authorName", "Author.LastName" }
    /// </code>
    /// This dictionary is intended for quick lookup and translation of legacy
    /// client parameters; prefer <see cref="AliasInfos"/> when you need
    /// descriptions or examples.
    /// </remarks>
    public IDictionary<string, string> Aliases { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Detailed metadata for aliases including a client-friendly alias,
    /// the real path, a human-readable description and an example.
    /// </summary>
    /// <remarks>
    /// Each <see cref="AliasMetadata"/> instance provides structured information
    /// that can be used to build documentation, validation messages, or UI hints.
    /// </remarks>
    public IEnumerable<AliasMetadata> AliasInfos { get; init; } = Array.Empty<AliasMetadata>();

    /// <summary>
    /// A list of property names or paths that are safe and supported for ordering results.
    /// </summary>
    /// <remarks>
    /// Consumers should restrict ORDER BY operations to these properties to avoid
    /// runtime errors or exposing internal state. Values are typically simple
    /// property names (e.g., <c>"Title"</c>) or aliased names that the server
    /// understands.
    /// </remarks>
    public IEnumerable<string> OrderableProperties { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Navigation property paths that are allowed to be included in queries (e.g. "Author.Address").
    /// </summary>
    /// <remarks>
    /// Use these paths to validate include/expand parameters coming from clients
    /// to prevent expensive or unsafe data loading.
    /// </remarks>
    public IEnumerable<string> IncludePaths { get; init; } = Array.Empty<string>();
}