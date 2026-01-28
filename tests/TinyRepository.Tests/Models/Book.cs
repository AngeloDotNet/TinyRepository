using TinyRepository.Sorting;

namespace TinyRepository.Tests.Models;

public class Book
{
    public int Id { get; set; }

    [IncludeAllowed]
    public Publisher? Publisher { get; set; }

    [Orderable]
    public DateTime PublishedAt { get; set; }

    [IncludeAllowed]
    public Author? Author { get; set; }
}