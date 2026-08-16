using KH2.ManagementSystem.Domain.FaceRecognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class FaceEnrollmentCaptureConfiguration : IEntityTypeConfiguration<FaceEnrollmentCapture>
{
    public void Configure(EntityTypeBuilder<FaceEnrollmentCapture> builder)
    {
        builder.ToTable("FaceEnrollmentCaptures");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Pose).HasMaxLength(40).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.EnrollmentId, x.Sequence }).IsUnique();
        builder.HasOne<FaceEnrollment>().WithMany().HasForeignKey(x => x.EnrollmentId).OnDelete(DeleteBehavior.Cascade);
    }
}
