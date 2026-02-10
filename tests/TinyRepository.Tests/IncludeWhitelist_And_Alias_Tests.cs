using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TinyRepository.Extensions;
using TinyRepository.Interfaces;
using TinyRepository.Tests.Data;
using TinyRepository.Tests.Models;

namespace TinyRepository.Tests;

public class IncludeWhitelistAndAliasTests
{
    [Fact]
    public async Task IncludeAlias_AllowsInclude_ByAlias_ButRejectsInvalidIncludeAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContext<IncludeDbContext>(opt => opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services.AddRepositoryPattern<IncludeDbContext>();
        services.AddAttributeWhitelistScan(opt => opt.MaxDepth = 2, Assembly.GetExecutingAssembly());

        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<IncludeDbContext>();
        var child = new Child { Name = "c1" };
        ctx.Children.Add(child);
        ctx.Parents.Add(new Parent { Child = child });
        await ctx.SaveChangesAsync();

        var repo = scope.ServiceProvider.GetRequiredService<IRepository<Parent, int>>();

        // include using alias should work
        var page = await repo.GetPagedTwoStageAsync(1, 10, null, null,
            false, null, true, TestContext.Current.CancellationToken, false, ["Child"]);

        Assert.Single(page.Items);
        Assert.NotNull(page.Items.First().Child);

        // invalid include should be rejected
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await repo.GetPagedTwoStageAsync(1, 10, includePaths: ["non_allowed"], cancellationToken: TestContext.Current.CancellationToken);
        });
    }
}