using TinyRepository.Sorting;

namespace TinyRepository.Tests.Models;

public class Tag
{
    public int Id { get; set; }

    [IncludeAllowed]
    public string? Name { get; set; }
}