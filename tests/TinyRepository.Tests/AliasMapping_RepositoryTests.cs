using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TinyRepository.Extensions;
using TinyRepository.Interfaces;
using TinyRepository.Tests.Data;
using TinyRepository.Tests.Models;

namespace TinyRepository.Tests;

// Test repository behavior when using an alias defined on an entity property.
public class AliasMapping_RepositoryTests
{
    [Fact]
    public async Task GetPagedTwoStage_WithOrderByAlias_WorksAndOrdersCorrectlyAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AliasDbContext>(opt => opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddRepositoryPattern<AliasDbContext>();

        // Scan current assembly so AliasEntity's attributes are discovered
        services.AddAttributeWhitelistScan(opt => opt.MaxDepth = 2, Assembly.GetExecutingAssembly());

        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AliasDbContext>();
        // seed
        ctx.AliasEntities.Add(new AliasEntity { Title = "C" });
        ctx.AliasEntities.Add(new AliasEntity { Title = "A" });
        ctx.AliasEntities.Add(new AliasEntity { Title = "B" });
        await ctx.SaveChangesAsync();

        var repo = scope.ServiceProvider.GetRequiredService<IRepository<AliasEntity, int>>();

        var page = await repo.GetPagedTwoStageAsync(1, 10, null, orderByProperty: "Title",
            descending: false, null, cancellationToken: TestContext.Current.CancellationToken);

        var titles = page.Items.Select(x => x.Title).ToList();
        Assert.Equal(new[] { "A", "B", "C" }, titles);
    }
}