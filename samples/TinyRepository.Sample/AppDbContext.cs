using Microsoft.EntityFrameworkCore;
using TinyRepository.Interfaces;
using TinyRepository.Sample.Entities;
using static TinyRepository.UnitOfWork<TinyRepository.Ef.AppDbContext>;

namespace TinyRepository.Ef;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public virtual DbSet<SampleEntity> SampleEntities { get; set; }
    public virtual DbSet<Author> Authors { get; set; }

    public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Database.BeginTransactionAsync(cancellationToken).ContinueWith<IUnitOfWorkTransaction>(t
            => new EfUnitOfWorkTransaction(t.Result), cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SampleEntity>(entity =>
        {
            // Mappa l'entità alla tabella "SampleEntities"
            entity.ToTable("SampleEntities");

            // Imposta la chiave primaria, se HasKey non viene chiamato, EF Core considera automaticamente la proprietà "Id" come chiave primaria
            // Per chiavi primarie composite, utilizzare entity.HasKey(e => new { e.KeyPart1, e.KeyPart2 });
            entity.HasKey(model => model.Id);
            entity.Property(model => model.Name).IsRequired().HasMaxLength(100);

            entity.HasOne(e => e.Author)
                  .WithMany()
                  .HasForeignKey("AuthorId")
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        //modelBuilder.Entity<SampleEntity>(entity =>
        //{
        //    entity.ToTable("SampleEntities");
        //    entity.HasKey(e => e.Id);
        //    entity.Property(e => e.Name).IsRequired().HasMaxLength(100);

        //    entity.OwnsOne(e => e.Author, a =>
        //    {
        //        a.Property(p => p.FirstName).HasMaxLength(100).HasColumnName("AuthorFirstName");
        //        a.Property(p => p.LastName).HasMaxLength(100).HasColumnName("AuthorLastName");
        //    });
        //});

        modelBuilder.Entity<Author>(entity =>
        {
            entity.ToTable("Authors");
            //entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        });
    }
}