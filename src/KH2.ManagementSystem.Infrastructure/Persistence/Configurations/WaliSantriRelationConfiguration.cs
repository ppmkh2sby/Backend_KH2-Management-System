using KH2.ManagementSystem.Domain.Walis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class WaliSantriRelationConfiguration : IEntityTypeConfiguration<WaliSantriRelation>
{
    public void Configure(EntityTypeBuilder<WaliSantriRelation> builder)
    {
        builder.ToTable("WaliSantriRelations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.WaliUserId)
            .IsRequired();

        builder.Property(x => x.SantriId)
            .IsRequired();

        builder.Property(x => x.RelationshipLabel)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasIndex(x => new { x.WaliUserId, x.SantriId })
            .IsUnique();

        builder.HasOne<Domain.Users.User>()
            .WithMany()
            .HasForeignKey(x => x.WaliUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Santris.Santri>()
            .WithMany()
            .HasForeignKey(x => x.SantriId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}