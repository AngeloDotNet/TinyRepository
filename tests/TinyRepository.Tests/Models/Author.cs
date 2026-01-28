using TinyRepository.Sorting;

namespace TinyRepository.Tests.Models;

public class Author
{
    public int Id { get; set; }

    [Orderable]
    [IncludeAllowed]
    public string? LastName { get; set; }

    [IncludeAllowed]
    public ICollection<Book>? Books { get; set; }
}