using TinyRepository.Entities;
using TinyRepository.Sorting;

namespace TinyRepository.Tests.Models;

public class Parent : IEntity<int>
{
    public int Id { get; set; }

    [IncludeAllowed]
    public Child? Child { get; set; }
}