namespace TinyRepository.Sorting;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class OrderableAttribute : Attribute
{
    public string? Alias { get; }

    public OrderableAttribute() { }
    public OrderableAttribute(string alias) => Alias = alias;
}