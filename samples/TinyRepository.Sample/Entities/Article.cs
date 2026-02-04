using TinyRepository.Entities;
using TinyRepository.Sorting;

namespace TinyRepository.Sample.Entities;

public class Article : IEntity<int>
{
    public int Id { get; set; }

    [Orderable]
    [IncludeAllowed]
    public string? Title { get; set; }

    public bool IsPublished { get; set; }

    [IncludeAllowed]
    public Author? Author { get; set; }

    [IncludeAllowed]
    public ICollection<Tag>? Tags { get; set; }
}