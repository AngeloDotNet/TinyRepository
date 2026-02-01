using TinyRepository.Entities;
using TinyRepository.Sorting;

namespace TinyRepository.Sample.Entities;

public class SampleEntity : IEntity<int>
{
    public int Id { get; set; }

    [Orderable]                 // consentito: "Name"
    public string? Name { get; set; }

    public DateTime CreatedAt { get; set; }

    // navigation example: nested property Author.LastName è considerata se LastName è decorata
    public Author? Author { get; set; }
}