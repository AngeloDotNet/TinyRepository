using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TinyRepository.Extensions;
using TinyRepository.Interfaces;
using TinyRepository.Tests.Data;
using TinyRepository.Tests.Models;

namespace TinyRepository.Tests;

public class AsSplitQuery_EqualityTests
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
    public async Task TwoStage_WithAndWithoutAsSplitQuery_ReturnSameResultsAsync()
    {
        var sp = BuildServices();
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // seed minimal authors/articles if needed
        if (!ctx.Authors.Any())
        {
            var author = new Author { LastName = "X", Books = new List<Book>() };
            author.Books.Add(new Book { PublishedAt = DateTime.UtcNow, Publisher = new Publisher { Name = "P1" }, Author = author });
            ctx.Authors.Add(author);
            ctx.Articles.Add(new Article { Title = "T1", IsPublished = true, Author = author, Tags = new System.Collections.Generic.List<Tag> { new Tag { Name = "t" } } });
            await ctx.SaveChangesAsync();
        }

        var repo = scope.ServiceProvider.GetRequiredService<IRepository<Article, int>>();

        var r1 = await repo.GetPagedTwoStageAsync(1, 10, includePaths: new[] { "Author", "Tags" }, useAsSplitQuery: false);
        var r2 = await repo.GetPagedTwoStageAsync(1, 10, includePaths: new[] { "Author", "Tags" }, useAsSplitQuery: true);

        Assert.Equal(r1.TotalCount, r2.TotalCount);
        Assert.Equal(r1.Items.Count(), r2.Items.Count());
    }
}