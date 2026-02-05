namespace TinyRepository.Metadata;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class EntityNameAttribute(string entityName) : Attribute
{
    public string EntityName { get; } = entityName ?? throw new ArgumentNullException(nameof(entityName));
}
