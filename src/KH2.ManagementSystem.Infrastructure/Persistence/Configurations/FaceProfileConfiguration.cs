using KH2.ManagementSystem.Domain.FaceRecognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class FaceProfileConfiguration : IEntityTypeConfiguration<FaceProfile>
{
    public void Configure(EntityTypeBuilder<FaceProfile> builder)
    {
        builder.ToTable("FaceProfiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ProviderProfileId).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.SantriId).IsUnique();
        builder.HasIndex(x => x.ProviderProfileId).IsUnique();
        builder.HasOne<Domain.Santris.Santri>().WithMany().HasForeignKey(x => x.SantriId).OnDelete(DeleteBehavior.Cascade);
    }
}
