using Inventory.Domain.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

internal sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");
        builder.HasKey(location => location.Id);

        builder.Property(location => location.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(location => location.Code).HasColumnName("code").IsRequired();
        builder.Property(location => location.Name).HasColumnName("name")
            .HasMaxLength(Location.MaxNameLength).IsRequired();
        builder.Property(location => location.ParentId).HasColumnName("parent_id");
        builder.Property(location => location.IsActive).HasColumnName("is_active");
        builder.Property(location => location.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(location => location.Code).IsUnique().HasDatabaseName("ux_locations_code");

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(location => location.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
