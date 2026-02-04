using TinyRepository.DTOs;

namespace TinyRepository.Metadata.Interfaces;

public interface IMetadataService
{
    /// <summary>
    /// Restituisce le informazioni di whitelist/alias per l'entità identificata da entityName.
    /// entityName può essere il nome semplice del tipo (es. "Article") o il fullname.
    /// </summary>
    Task<EntityWhitelistDto?> GetEntityWhitelistAsync(string entityName);
}