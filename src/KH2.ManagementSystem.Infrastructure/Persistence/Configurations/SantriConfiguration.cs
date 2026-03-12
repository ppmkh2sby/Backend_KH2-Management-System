using KH2.ManagementSystem.Domain.Santris;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class SantriConfiguration : IEntityTypeConfiguration<Santri>
{
    public void Configure(EntityTypeBuilder<Santri> builder)
    {
        builder.ToTable("Santris");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Nis)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Kampus)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Jurusan)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Gender)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Tim)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Kelas)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Catatan)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasIndex(x => x.Nis)
            .IsUnique();

        builder.HasOne<Domain.Users.User>()
            .WithOne()
            .HasForeignKey<Santri>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}