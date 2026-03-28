using KH2.ManagementSystem.Domain.LogKeluarMasuks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class LogKeluarMasukConfiguration : IEntityTypeConfiguration<LogKeluarMasuk>
{
    public void Configure(EntityTypeBuilder<LogKeluarMasuk> builder)
    {
        builder.ToTable("LogKeluarMasuks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.SantriId)
            .IsRequired();

        builder.Property(x => x.TanggalPengajuan)
            .IsRequired();

        builder.Property(x => x.Jenis)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Rentang)
            .HasMaxLength(255);

        builder.Property(x => x.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Petugas)
            .HasMaxLength(255);

        builder.Property(x => x.Catatan);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasIndex(x => new { x.SantriId, x.TanggalPengajuan });
        builder.HasIndex(x => x.TanggalPengajuan);
        builder.HasIndex(x => x.Status);

        builder.HasOne<Domain.Santris.Santri>()
            .WithMany()
            .HasForeignKey(x => x.SantriId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
