using TinyRepository.Entities;

namespace TinyRepository.Tests.Models;

public class CompositeEntity : IEntity<int>
{
    public int Id { get; set; }
    public int Key1 { get; set; }
    public int Key2 { get; set; }
    public string? Name { get; set; }
}