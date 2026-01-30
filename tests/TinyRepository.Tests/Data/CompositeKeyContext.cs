using Microsoft.EntityFrameworkCore;
using TinyRepository.Tests.Models;

namespace TinyRepository.Tests.Data;

public class CompositeKeyContext(DbContextOptions<CompositeKeyContext> options) : DbContext(options)
{
    public DbSet<CompositeEntity> CompositeEntities => Set<CompositeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompositeEntity>().HasKey(e => new { e.Key1, e.Key2 });
    }
}