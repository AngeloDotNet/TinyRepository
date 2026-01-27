using TinyRepository.Extensions;
using TinyRepository.Provider;
using TinyRepository.Provider.Interfaces;

namespace TinyRepository.Sorting.Provider;

public class AttributeWhitelistProvider<T> : IPropertyWhitelistProvider<T>, IIncludeWhitelistProvider<T>
{
    public IEnumerable<string> GetAllowedProperties()
    {
        return OrderablePropertyScanner.GetOrderableProperties<T>();
    }

    public IEnumerable<string> GetAllowedIncludePaths()
    {
        return IncludePathScanner.GetIncludePaths<T>();
    }
}
