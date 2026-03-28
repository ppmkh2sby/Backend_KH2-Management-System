using KH2.ManagementSystem.Domain.Kegiatans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class KegiatanConfiguration : IEntityTypeConfiguration<Kegiatan>
{
    public void Configure(EntityTypeBuilder<Kegiatan> builder)
    {
        builder.ToTable("Kegiatans");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Kategori)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Waktu)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Catatan);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasIndex(x => new { x.Kategori, x.Waktu })
            .IsUnique();
    }
}
