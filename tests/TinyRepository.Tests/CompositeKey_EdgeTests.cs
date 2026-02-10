using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TinyRepository.Extensions;
using TinyRepository.Interfaces;
using TinyRepository.Tests.Data;
using TinyRepository.Tests.Models;

namespace TinyRepository.Tests;

public class CompositeKey_EdgeTests
{
    [Fact]
    public async Task GetPagedTwoStage_CompositeKey_Throws_NotSupportedExceptionAsync()
    {
        var services = new ServiceCollection();

        services.AddDbContext<CompositeKeyContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddRepositoryPattern<CompositeKeyContext>();

        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<CompositeEntity, int>>();
        await Assert.ThrowsAsync<NotSupportedException>(async () => await repo.GetPagedTwoStageAsync(1, 10));
    }
}