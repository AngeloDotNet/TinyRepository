using TinyRepository.Sorting;

namespace TinyRepository.Sample.Entities;

public class Tag
{
    public int Id { get; set; }

    [IncludeAllowed]
    public string? Name { get; set; }
}