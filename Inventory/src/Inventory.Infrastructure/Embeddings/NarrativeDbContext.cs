using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Inventory.Infrastructure.Embeddings;

/// <summary>
/// O EF cacheia o modelo compilado por tipo de contexto. Como a dimensão do vetor vem
/// de configuração e faz parte do modelo, ela precisa entrar na chave do cache — senão
/// o primeiro contexto criado no processo dita a dimensão de todos os outros.
/// </summary>
internal sealed class NarrativeModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        context is NarrativeDbContext narrative
            ? (context.GetType(), narrative.VectorDimensions, designTime)
            : (context.GetType(), 0, designTime);
}

/// <summary>
/// Contexto próprio para o índice semântico, separado do <c>InventoryDbContext</c>.
/// </summary>
/// <remarks>
/// A separação não é purismo. Ela dá três coisas concretas:
/// o inventário não passa a exigir a extensão <c>vector</c> para funcionar;
/// a dimensão do vetor pode vir de configuração sem contaminar o modelo principal;
/// e o índice pode viver em outro banco, já que ele é reconstruível a partir do razão.
/// </remarks>
internal sealed class NarrativeDbContext(
    DbContextOptions<NarrativeDbContext> options, EmbeddingOptions embeddings) : DbContext(options)
{
    public DbSet<ItemNarrativeRecord> Narratives => Set<ItemNarrativeRecord>();

    internal int VectorDimensions => embeddings.Dimensions;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, NarrativeModelCacheKeyFactory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<ItemNarrativeRecord>(entity =>
        {
            entity.ToTable("item_narratives");
            entity.HasKey(record => record.LedgerEntryId);

            entity.Property(record => record.LedgerEntryId).HasColumnName("ledger_entry_id").ValueGeneratedNever();
            entity.Property(record => record.ItemId).HasColumnName("item_id");
            entity.Property(record => record.Sku).HasColumnName("sku").HasMaxLength(64);
            entity.Property(record => record.OccurredAt).HasColumnName("occurred_at");
            entity.Property(record => record.Text).HasColumnName("text");
            entity.Property(record => record.Embedding).HasColumnName("embedding")
                .HasColumnType($"vector({embeddings.Dimensions})");

            entity.HasIndex(record => record.ItemId).HasDatabaseName("ix_item_narratives_item");
        });
    }
}
