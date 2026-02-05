using TinyRepository.Metadata;

namespace TinyRepository.DTOs;

public class EntityWhitelistDto
{
    public string EntityType { get; init; } = string.Empty;

    /// <summary>
    /// Alias -> actualPath (es. "authorName" => "Author.LastName")
    /// kept for backward compatibility
    /// </summary>
    public IDictionary<string, string> Aliases { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Dettagli completi sugli alias: alias-friendly, path reale, description, example.
    /// </summary>
    public IEnumerable<AliasMetadata> AliasInfos { get; init; } = Array.Empty<AliasMetadata>();

    public IEnumerable<string> OrderableProperties { get; init; } = Array.Empty<string>();
    public IEnumerable<string> IncludePaths { get; init; } = Array.Empty<string>();
}