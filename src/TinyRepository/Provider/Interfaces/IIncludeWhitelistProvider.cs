namespace TinyRepository.Provider.Interfaces;

public interface IIncludeWhitelistProvider<T>
{
    IEnumerable<string> GetAllowedIncludePaths();
}