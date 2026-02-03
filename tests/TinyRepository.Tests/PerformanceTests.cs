using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TinyRepository.Extensions;
using TinyRepository.Interfaces;
using TinyRepository.Tests.Data;
using TinyRepository.Tests.Models;

namespace TinyRepository.Tests;

public class PerformanceTests(ITestOutputHelper outp)
{
    private ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(opt => opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddRepositoryPattern<TestDbContext>();
        services.AddAttributeWhitelistScan(opt => opt.MaxDepth = 4, typeof(Article).Assembly);

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task AsSplitQuery_vs_Default_ComparisonAsync()
    {
        var sp = BuildServices();
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // seed moderate data
        for (var i = 0; i < 400; i++)
        {
            var a = new Author { LastName = $"Author{i % 20}" };
            a.Books = Enumerable.Range(0, 5).Select(j
                => new Book { PublishedAt = DateTime.UtcNow.AddDays(-(i + j)), Publisher = new Publisher { Name = $"Pub{j}" }, Author = a }).ToList();

            ctx.Authors.Add(a);
        }

        await ctx.SaveChangesAsync();

        var repo = scope.ServiceProvider.GetRequiredService<IRepository<Author, int>>();

        // warmup
        await repo.GetPagedTwoStageAsync(1, 10, sortDescriptors: new[] { Sorting.SortDescriptor.Asc("LastName") }, includePaths: new[] { "Books", "Books.Publisher" });

        // measure default
        var sw = Stopwatch.StartNew();
        var resDefault = await repo.GetPagedTwoStageAsync(2, 20, sortDescriptors: new[] { Sorting.SortDescriptor.Asc("LastName") }, includePaths: new[] { "Books", "Books.Publisher" }, useAsSplitQuery: false);
        sw.Stop();
        var tDefault = sw.Elapsed;

        // measure AsSplitQuery
        sw.Restart();
        var resSplit = await repo.GetPagedTwoStageAsync(2, 20, sortDescriptors: new[] { Sorting.SortDescriptor.Asc("LastName") }, includePaths: new[] { "Books", "Books.Publisher" }, useAsSplitQuery: true);
        sw.Stop();
        var tSplit = sw.Elapsed;

        outp.WriteLine($"Default duration: {tDefault.TotalMilliseconds} ms - AsSplitQuery duration: {tSplit.TotalMilliseconds} ms");

        Assert.Equal(resDefault.TotalCount, resSplit.TotalCount);
        Assert.Equal(resDefault.PageSize, resSplit.PageSize);
        Assert.Equal(resDefault.PageNumber, resSplit.PageNumber);

        // don't assert which is faster (InMemory provider is not representative), only ensure both completed and returned items.
        Assert.True(resDefault.Items.Any());
        Assert.True(resSplit.Items.Any());
    }
}