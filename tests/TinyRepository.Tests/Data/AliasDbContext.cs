using Microsoft.EntityFrameworkCore;
using TinyRepository.Tests.Models;

namespace TinyRepository.Tests.Data;

public class AliasDbContext : DbContext
{
    public AliasDbContext(DbContextOptions<AliasDbContext> options) : base(options) { }
    public DbSet<AliasEntity> AliasEntities => Set<AliasEntity>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AliasEntity>().HasKey(e => e.Id);
    }
}