using TinyRepository.Entities;
using TinyRepository.Sample.Models;
using TinyRepository.Sorting;

namespace TinyRepository.Samples.Models;

public class SampleEntity : IEntity<int>
{
    public int Id { get; set; }

    [Orderable]                 // consentito: "Name"
    public string? Name { get; set; }

    public DateTime CreatedAt { get; set; }

    // navigation example: nested property Author.LastName è considerata se LastName è decorata
    public Author? Author { get; set; }
}