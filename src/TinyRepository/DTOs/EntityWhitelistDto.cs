namespace TinyRepository.DTOs;

public class EntityWhitelistDto
{
    public string EntityType { get; init; } = string.Empty;

    /// <summary>
    /// Alias -> actualPath (es. "authorName" => "Author.LastName")
    /// </summary>
    public IDictionary<string, string> Aliases { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Orderable properties using actual paths (e.g. "Author.LastName", "Title")
    /// </summary>
    public IEnumerable<string> OrderableProperties { get; init; } = [];

    /// <summary>
    /// Include allowed paths (actual paths) (e.g. "Author", "Author.Books")
    /// </summary>
    public IEnumerable<string> IncludePaths { get; init; } = [];
}
