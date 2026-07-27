using Microsoft.EntityFrameworkCore;
using MyPgVectorStore.Models;

namespace MyPgVectorStore.Data;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Recomendation> Recomendations { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Recomendation>(entity =>
        {
            entity.ToTable("recomendations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.Embedding).HasColumnName("embedding").HasColumnType("vector(1024)");
        });
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.Summary).HasColumnName("summary");
            entity.Property(e => e.Description).HasColumnName("description");
        });
    }
}
