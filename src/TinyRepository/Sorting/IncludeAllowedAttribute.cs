namespace TinyRepository.Sorting;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class IncludeAllowedAttribute : Attribute
{
    public string? Alias { get; }
    public string? Description { get; }
    public string? Example { get; }

    public IncludeAllowedAttribute() { }
    public IncludeAllowedAttribute(string alias) => Alias = alias;
    public IncludeAllowedAttribute(string alias, string description = null, string example = null)
    {
        Alias = alias;
        Description = description;
        Example = example;
    }
}