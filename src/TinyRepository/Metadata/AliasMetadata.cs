namespace TinyRepository.Metadata;

public sealed class AliasMetadata
{
    public string Alias { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Example { get; init; }
}