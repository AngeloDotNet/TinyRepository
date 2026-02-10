using TinyRepository.Entities;
using TinyRepository.Sorting;

namespace TinyRepository.Tests.Models;

public class AliasEntity : IEntity<int>
{
    public int Id { get; set; }

    [Orderable]
    public string? Title { get; set; }
}