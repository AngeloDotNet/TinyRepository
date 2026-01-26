using TinyRepository.Sorting;

namespace TinyRepository.Sample.Models;

public class Author
{
    [Orderable]                 // consentito: Author.LastName
    public string? LastName { get; set; }

    [Orderable]                 // consentito: Author.FirstName
    public string? FirstName { get; set; }
}