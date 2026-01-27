namespace TinyRepository.Sorting;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class IncludeAllowedAttribute : Attribute
{
    /// <summary>
    /// Opzionale alias per il nome dell'include path (se si desidera esporre un alias).
    /// </summary>
    public string? Alias { get; }

    public IncludeAllowedAttribute() { }
    public IncludeAllowedAttribute(string alias) => Alias = alias;
}