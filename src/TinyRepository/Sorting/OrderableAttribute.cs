namespace TinyRepository.Sorting;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class OrderableAttribute : Attribute
{
    public string? Alias { get; }
    public string? Description { get; }
    public string? Example { get; }

    public OrderableAttribute() { }
    public OrderableAttribute(string alias) => Alias = alias;
    public OrderableAttribute(string alias, string description = null, string example = null)
    {
        Alias = alias;
        Description = description;
        Example = example;
    }
}