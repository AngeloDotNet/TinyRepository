using TinyRepository.DTOs;

namespace TinyRepository.Metadata.Interfaces;

public interface IMetadataService
{
    Task<EntityWhitelistDto?> GetEntityWhitelistAsync(string entityName);
    Task<IEnumerable<string>> GetAllEntityNamesAsync();
}