using TinyRepository.Entities;
using TinyRepository.Sorting;

namespace TinyRepository.Sample.Entities;

public class Author : IEntity<int>
{
    public int Id { get; set; }

    [Orderable]  // consentito: Author.LastName
    public string? LastName { get; set; }

    [Orderable]  // consentito: Author.FirstName
    public string? FirstName { get; set; }
}