using Microsoft.Extensions.DependencyInjection;
using TinyRepository.Extensions;
using TinyRepository.Metadata.Interfaces;
using TinyRepository.Tests.Models;

namespace TinyRepository.Tests;

public class MetadataService_MaxDepth_ReuseTests
{
    [Fact]
    public async Task AddAttributeWhitelistScan_MaxDepth_IsReusedBy_MetadataService_WhenNotSpecifiedAsync()
    {
        var services = new ServiceCollection();
        // register scan with maxDepth = 1 (shallow)
        services.AddAttributeWhitelistScan(opt => opt.MaxDepth = 1, typeof(Article).Assembly);

        // register metadata service WITHOUT specifying MaxDepth (should reuse above)
        services.AddMetadataService(null, typeof(Article).Assembly);

        var sp = services.BuildServiceProvider();

        var metadata = sp.GetRequiredService<IMetadataService>();

        // Article has nested allowed includes (Author, Author.Books, Author.Books.Publisher)
        var dto = await metadata.GetEntityWhitelistAsync("Article");
        Assert.NotNull(dto);

        // With MaxDepth = 1 deep nested path "Author.Books.Publisher" should NOT appear
        Assert.DoesNotContain("Author.Books.Publisher", dto.IncludePaths);
    }
}