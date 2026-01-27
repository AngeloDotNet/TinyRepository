namespace TinyRepository.Provider.Interfaces;

public interface IPropertyWhitelistProvider<T>
{
    IEnumerable<string> GetAllowedProperties();
}