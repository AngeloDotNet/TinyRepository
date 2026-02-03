using TinyRepository.Entities;
using TinyRepository.Sorting;

namespace TinyRepository.Tests.Models;

public class Author : IEntity<int>
{
    public int Id { get; set; }

    [Orderable]
    [IncludeAllowed]
    public string? LastName { get; set; }

    [IncludeAllowed]
    public ICollection<Book>? Books { get; set; }
}