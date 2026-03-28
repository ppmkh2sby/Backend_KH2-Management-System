using KH2.ManagementSystem.Domain.Presensis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class PresensiConfiguration : IEntityTypeConfiguration<Presensi>
{
    public void Configure(EntityTypeBuilder<Presensi> builder)
    {
        builder.ToTable("Presensis");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.SantriId)
            .IsRequired();

        builder.Property(x => x.Nama)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.KegiatanId)
            .IsRequired();

        builder.Property(x => x.SesiId);

        builder.Property(x => x.Catatan);

        builder.Property(x => x.Waktu)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.UpdatedAtUtc);
        builder.HasIndex(x => new { x.SantriId, x.Status });
        builder.HasIndex(x => new { x.SantriId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.SesiId, x.SantriId });
        builder.HasIndex(x => new { x.SesiId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.KegiatanId, x.Waktu });
        builder.HasIndex(x => x.CreatedAtUtc)
            .HasFilter("\"SesiId\" IS NULL")
            .HasDatabaseName("IX_Presensis_CreatedAtUtc_LegacyNullSesi");

        builder.HasOne<Domain.Santris.Santri>()
            .WithMany()
            .HasForeignKey(x => x.SantriId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Domain.Kegiatans.Kegiatan>()
            .WithMany()
            .HasForeignKey(x => x.KegiatanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Domain.Sesis.Sesi>()
            .WithMany()
            .HasForeignKey(x => x.SesiId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
