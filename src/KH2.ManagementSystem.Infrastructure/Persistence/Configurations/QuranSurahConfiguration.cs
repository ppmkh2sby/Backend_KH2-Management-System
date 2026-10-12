using KH2.ManagementSystem.Domain.Quran;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class QuranSurahConfiguration : IEntityTypeConfiguration<QuranSurah>
{
    public void Configure(EntityTypeBuilder<QuranSurah> builder)
    {
        builder.ToTable("QuranSurahs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Number)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ArabicName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.VerseCount)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasIndex(x => x.Number)
            .IsUnique();
    }
}
