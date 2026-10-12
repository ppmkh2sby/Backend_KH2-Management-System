using KH2.ManagementSystem.Domain.JurnalKeilmuans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class JurnalKeilmuanConfiguration : IEntityTypeConfiguration<JurnalKeilmuan>
{
    public void Configure(EntityTypeBuilder<JurnalKeilmuan> builder)
    {
        builder.ToTable("JurnalKeilmuans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SesiSambung).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Kelas).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NamaDewanGuru).HasMaxLength(150).IsRequired();
        builder.Property(x => x.JenisMateri).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DibuatOleh).HasMaxLength(200).IsRequired();
        builder.Property(x => x.QuranKeterangan).HasMaxLength(1000);
        builder.Property(x => x.DetailsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.HasIndex(x => x.Tanggal);
        builder.HasIndex(x => new { x.Tanggal, x.JenisMateri });
        builder.HasOne<Domain.Quran.QuranSurah>().WithMany().HasForeignKey(x => x.QuranTargetSurahId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Domain.Quran.QuranSurah>().WithMany().HasForeignKey(x => x.QuranRealisasiSurahId).OnDelete(DeleteBehavior.Restrict);
    }
}
