using KH2.ManagementSystem.Domain.ProgressKeilmuans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class ProgressKeilmuanConfiguration : IEntityTypeConfiguration<ProgressKeilmuan>
{
    public void Configure(EntityTypeBuilder<ProgressKeilmuan> builder)
    {
        builder.ToTable("ProgressKeilmuans");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.SantriId)
            .IsRequired();

        builder.Property(x => x.Judul)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Target)
            .IsRequired();

        builder.Property(x => x.Capaian)
            .IsRequired();

        builder.Property(x => x.Satuan)
            .HasMaxLength(30);

        builder.Property(x => x.Level)
            .HasMaxLength(50);

        builder.Property(x => x.Catatan);

        builder.Property(x => x.Pembimbing)
            .HasMaxLength(100);

        builder.Property(x => x.TerakhirSetorUtc);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasIndex(x => new { x.SantriId, x.Judul });
        builder.HasIndex(x => new { x.SantriId, x.UpdatedAtUtc });
        builder.HasIndex(x => x.Level);
        builder.HasIndex(x => new { x.Level, x.SantriId });

        builder.HasOne<Domain.Santris.Santri>()
            .WithMany()
            .HasForeignKey(x => x.SantriId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
