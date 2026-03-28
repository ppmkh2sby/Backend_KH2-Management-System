using KH2.ManagementSystem.Domain.Kafarahs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class KafarahConfiguration : IEntityTypeConfiguration<Kafarah>
{
    public void Configure(EntityTypeBuilder<Kafarah> builder)
    {
        builder.ToTable("Kafarahs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.SantriId)
            .IsRequired();

        builder.Property(x => x.Tanggal)
            .IsRequired();

        builder.Property(x => x.JenisPelanggaran)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.KafarahText)
            .HasColumnName("Kafarah")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.JumlahSetor)
            .IsRequired();

        builder.Property(x => x.Tanggungan)
            .IsRequired();

        builder.Property(x => x.Tenggat);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasIndex(x => new { x.SantriId, x.Tanggal });

        builder.HasOne<Domain.Santris.Santri>()
            .WithMany()
            .HasForeignKey(x => x.SantriId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
