using Microsoft.EntityFrameworkCore;
using SearchEngineProject.Analysis;
using SearchEngineProject.Persistence.Entities;

namespace SearchEngineProject.Persistence;

public class SearchEngineContext : DbContext
{
    public DbSet<DocumentEntity> Documents { get; set; }
    public DbSet<DocumentTokenEntity> DocumentTokens { get; set; }

    public SearchEngineContext(DbContextOptions<SearchEngineContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // DocumentEntity
        modelBuilder.Entity<DocumentEntity>(b =>
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(200);
            // DocumentEntity-DocumentTokenEntity relationship
            b.HasMany(d => d.Tokens)
             .WithOne(t => t.Document)
             .HasForeignKey(t => t.DocumentId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        // DocumentTokenEntity
        modelBuilder.Entity<DocumentTokenEntity>(b =>
        {
            b.HasKey(t => new { t.DocumentId, t.Position });
            b.Property(t => t.Term)
             .IsRequired()
             .HasMaxLength(200);
            b.Property(t => t.StartOffset)
             .IsRequired();
            b.Property(t => t.EndOffset)
             .IsRequired();
        });
    }
}

