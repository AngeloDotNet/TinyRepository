using TinyRepository.Sorting;

namespace TinyRepository.Tests.Models;

public class Publisher
{
    public int Id { get; set; }

    [IncludeAllowed]
    [Orderable]
    public string? Name { get; set; }
}