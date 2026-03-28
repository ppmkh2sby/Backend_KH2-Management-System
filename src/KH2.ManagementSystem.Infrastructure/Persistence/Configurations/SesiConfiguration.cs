using KH2.ManagementSystem.Domain.Sesis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class SesiConfiguration : IEntityTypeConfiguration<Sesi>
{
    public void Configure(EntityTypeBuilder<Sesi> builder)
    {
        builder.ToTable("Sesis");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.KegiatanId)
            .IsRequired();

        builder.Property(x => x.Tanggal)
            .IsRequired();

        builder.Property(x => x.Catatan)
            .HasMaxLength(255);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasIndex(x => x.Tanggal);

        builder.HasOne<Domain.Kegiatans.Kegiatan>()
            .WithMany()
            .HasForeignKey(x => x.KegiatanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
