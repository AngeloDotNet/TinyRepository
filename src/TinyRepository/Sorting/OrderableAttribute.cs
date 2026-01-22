namespace TinyRepository.Sorting;

/// <summary>
/// Marca una proprietà come ordinabile (includibile nella whitelist dinamica).
/// Puoi applicarlo su proprietà primitive o navigation properties.
/// Per proprietà annidate usare [Orderable] sulla proprietà finale (es. Author.LastName).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class OrderableAttribute : Attribute
{
    /// <summary>
    /// Opzionale: permette di fornire un alias per la proprietà (es. "authorName").
    /// Se non specificato viene usato il nome della proprietà.
    /// </summary>
    public string? Alias { get; }
    public OrderableAttribute() { }
    public OrderableAttribute(string alias) => Alias = alias;
}
