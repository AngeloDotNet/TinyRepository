namespace TinyRepository.Sample.DTOs;

public class EntityRequest
{
    public int Id { get; set; }
    public string[]? Include { get; set; }
}