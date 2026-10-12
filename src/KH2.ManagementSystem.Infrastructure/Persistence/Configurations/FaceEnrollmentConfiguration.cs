using KH2.ManagementSystem.Domain.FaceRecognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class FaceEnrollmentConfiguration : IEntityTypeConfiguration<FaceEnrollment>
{
    public void Configure(EntityTypeBuilder<FaceEnrollment> builder)
    {
        builder.ToTable("FaceEnrollments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.FaceProfileId).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ModelName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ModelVersion).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ReferenceImagePath).HasMaxLength(500);
        builder.Property(x => x.EnrolledAtUtc).IsRequired();
        builder.HasIndex(x => x.FaceProfileId);
        builder.HasIndex(x => new { x.FaceProfileId, x.Status }).IsUnique()
            .HasFilter("\"Status\" = 'Active'").HasDatabaseName("UX_FaceEnrollments_Active_FaceProfile");
        builder.HasOne<FaceProfile>().WithMany().HasForeignKey(x => x.FaceProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}
