using TinyRepository.Extensions;
using TinyRepository.Tests.Models;

namespace TinyRepository.Tests;

public class ScannerCachingTests
{
    [Fact]
    public void OrderableScanner_IsIdempotent_And_Cached()
    {
        var first = OrderablePropertyScanner.GetOrderablePropertiesWithAlias(typeof(Article), 4).ToArray();
        var second = OrderablePropertyScanner.GetOrderablePropertiesWithAlias(typeof(Article), 4).ToArray();

        Assert.Equal(first.Length, second.Length);
        for (int i = 0; i < first.Length; i++)
        {
            Assert.Equal(first[i].Alias, second[i].Alias);
            Assert.Equal(first[i].Path, second[i].Path);
        }
    }

    [Fact]
    public void IncludeScanner_IsIdempotent_And_Cached()
    {
        var first = IncludePathScanner.GetIncludePathsWithAlias(typeof(Article), 4).ToArray();
        var second = IncludePathScanner.GetIncludePathsWithAlias(typeof(Article), 4).ToArray();

        Assert.Equal(first.Length, second.Length);
        for (var i = 0; i < first.Length; i++)
        {
            Assert.Equal(first[i].Alias, second[i].Alias);
            Assert.Equal(first[i].Path, second[i].Path);
        }
    }
}