using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TinyRepository.Extensions;
using TinyRepository.Interfaces;
using TinyRepository.Sorting;
using TinyRepository.Tests.Data;
using TinyRepository.Tests.Models;

namespace TinyRepository.Tests;

public class RepositoryIntegrationTests
{
    private ServiceProvider BuildServices(int maxDepth = 5)
    {
        var services = new ServiceCollection();

        services.AddDbContext<TestDbContext>(opt => opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddRepositoryPattern<TestDbContext>();

        services.AddAttributeWhitelistScan(opt => opt.MaxDepth = maxDepth, typeof(Article).Assembly);

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task TwoStagePaging_IncludesCollections_ReturnsCorrectPageAndIncludesAsync()
    {
        var sp = BuildServices();
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // seed
        var authors = Enumerable.Range(1, 5).Select(i => new Author
        {
            LastName = $"Author{i}",
            Books = []
        }).ToList();

        for (var i = 0; i < authors.Count; i++)
        {
            var a = authors[i];
            for (var b = 0; b < 3; b++)
            {
                a.Books!.Add(new Book { PublishedAt = DateTime.UtcNow.AddDays(-(i * 3 + b)), Publisher = new Publisher { Name = $"Pub{i}" }, Author = a });
            }
        }

        ctx.Authors.AddRange(authors);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // create articles with authors and tags
        var articles = Enumerable.Range(1, 30).Select(i =>
            new Article
            {
                Title = $"Article {i:D2}",
                IsPublished = i % 2 == 0,
                Author = authors[i % authors.Count],
                Tags = [new Tag { Name = "t1" }, new Tag { Name = "t2" }]
            }).ToList();

        ctx.Articles.AddRange(articles);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo = scope.ServiceProvider.GetRequiredService<IRepository<Article, int>>();

        // request page 2 size 5 sorted by Author.LastName desc then Title asc, include Author and Tags
        var descriptors = new[] { SortDescriptor.Desc("Author.LastName"), SortDescriptor.Asc("Title") };
        var page = await repo.GetPagedTwoStageAsync(pageNumber: 2, pageSize: 5, sortDescriptors: descriptors, filter: a
            => a.IsPublished, asNoTracking: true, includePaths: ["Author", "Tags"], cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(15, page.TotalCount); // 30 articles, half published => 15
        Assert.Equal(5, page.Items.Count());
        // includes loaded
        Assert.All(page.Items, item => Assert.NotNull(item.Author));
        Assert.All(page.Items, item => Assert.NotNull(item.Tags));
    }

    [Fact]
    public async Task UnitOfWork_Transaction_CommitsAndRollsBackCorrectlyAsync()
    {
        var sp = BuildServices();
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<Article, int>>();

        // initial
        ctx.Articles.RemoveRange(ctx.Articles);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // transaction commit
        await using (var tx = await uow.BeginTransactionAsync(TestContext.Current.CancellationToken))
        {
            await repo.AddAsync(new Article { Title = "TxArticle", IsPublished = true }, TestContext.Current.CancellationToken);
            await uow.SaveChangesAsync(TestContext.Current.CancellationToken);
            await tx.CommitAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(1, await ctx.Articles.CountAsync(TestContext.Current.CancellationToken));

        // transaction rollback
        await using (var tx2 = await uow.BeginTransactionAsync(TestContext.Current.CancellationToken))
        {
            await repo.AddAsync(new Article { Title = "RollbackArticle", IsPublished = false }, TestContext.Current.CancellationToken);
            await uow.SaveChangesAsync(TestContext.Current.CancellationToken);
            await tx2.RollbackAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(2, await ctx.Articles.CountAsync(TestContext.Current.CancellationToken)); // still 1
    }

    [Fact]
    public async Task IncludeWhitelist_PreventsInvalidIncludeAsync()
    {
        var sp = BuildServices();
        using var scope = sp.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<Article, int>>();

        // this should throw because "NonAllowed.Path" is not in include whitelist
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await repo.GetPagedAsync(1, 5, [SortDescriptor.Asc("Title")], filter: null, asNoTracking: true, includePaths: ["NonAllowed.Path"]);
        });
    }

    [Fact]
    public async Task GetPagedTwoStage_CompositeKey_ThrowsNotSupported()
    {
        // build a context with an entity that has composite key
        var services = new ServiceCollection();
        services.AddDbContext<CompositeKeyContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddRepositoryPattern<CompositeKeyContext>();

        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<CompositeKeyContext>();
        ctx.Add(new CompositeEntity { Key1 = 1, Key2 = 1, Name = "c" });

        await ctx.SaveChangesAsync();

        var repo = scope.ServiceProvider.GetRequiredService<IRepository<CompositeEntity, int>>(); // note: generic TKey doesn't match composite, but test aims to exercise exception

        await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await repo.GetPagedTwoStageAsync(1, 10);
        });
    }

    [Fact]
    public async Task GetPagedTwoStage_InvalidOrderProperty_ThrowsArgumentExceptionAsync()
    {
        var sp = BuildServices();
        using var scope = sp.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<Article, int>>();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await repo.GetPagedTwoStageAsync(1, 5, orderByProperty: "NonExistingProp");
        });
    }

    [Fact]
    public async Task MaxDepthExceeded_IncludeRejected()
    {
        // configure scan with maxDepth = 1 so nested include "Author.Books.Publisher" is beyond depth
        var sp = BuildServices(maxDepth: 1);
        using var scope = sp.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<Article, int>>();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await repo.GetPagedAsync(1, 5, [SortDescriptor.Asc("Title")], filter: null, asNoTracking: true, includePaths: ["Author.Books.Publisher"]);
        });
    }
}