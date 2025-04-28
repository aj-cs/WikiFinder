using Microsoft.EntityFrameworkCore;
using SearchEngineProject.Analysis;
using SearchEngineProject.Persistence.Entities;

namespace SearchEngineProject.Persistence;

public class SearchEngineContext : DbContext
{
    public DbSet<DocumentEntity> Documents { get; set; }
    public DbSet<DocumentTermEntity> DocumentTerms { get; set; }

    public SearchEngineContext(DbContextOptions<SearchEngineContext> opts)
      : base(opts)
    { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<DocumentEntity>(b =>
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.Title)
             .IsRequired()
             .HasMaxLength(200);
            b.HasMany(d => d.Tokens)
             .WithOne(t => t.Document)
             .HasForeignKey(t => t.DocumentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DocumentTermEntity>(b =>
        {
            // composite primary key-> one row per (DocumentId,Term)
            b.HasKey(t => new { t.DocumentId, t.Term });

            b.Property(t => t.Term)
             .IsRequired()
             .HasMaxLength(200);

            b.Property(t => t.PositionsJson)
             .IsRequired();
        });
    }
}

