using TinyRepository.Enums;

namespace TinyRepository.Sorting;

public sealed class SortDescriptor(string propertyName, SortDirection direction = SortDirection.Ascending)
{
    public string PropertyName { get; } = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
    public SortDirection Direction { get; } = direction;

    public static SortDescriptor Asc(string propertyName) => new SortDescriptor(propertyName, SortDirection.Ascending);
    public static SortDescriptor Desc(string propertyName) => new SortDescriptor(propertyName, SortDirection.Descending);
}
