using Microsoft.EntityFrameworkCore;
using TinyRepository.Tests.Models;

namespace TinyRepository.Tests.Data;

public class IncludeDbContext(DbContextOptions<IncludeDbContext> options) : DbContext(options)
{
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<Child> Children => Set<Child>();
}