using KH2.ManagementSystem.Domain.FaceRecognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class FaceRecognitionEventConfiguration : IEntityTypeConfiguration<FaceRecognitionEvent>
{
    public void Configure(EntityTypeBuilder<FaceRecognitionEvent> builder)
    {
        builder.ToTable("FaceRecognitionEvents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.FailureReason).HasMaxLength(100);
        builder.Property(x => x.ProcessingDurationMs).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.DeviceId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.SesiId, x.SantriId });
        builder.HasIndex(x => x.FaceProfileId);
        builder.HasIndex(x => x.FaceEnrollmentId);
        builder.HasOne<AttendanceDevice>().WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<FaceProfile>().WithMany().HasForeignKey(x => x.FaceProfileId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<FaceEnrollment>().WithMany().HasForeignKey(x => x.FaceEnrollmentId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Domain.Santris.Santri>().WithMany().HasForeignKey(x => x.SantriId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Domain.Sesis.Sesi>().WithMany().HasForeignKey(x => x.SesiId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Domain.Presensis.Presensi>().WithMany().HasForeignKey(x => x.PresensiId).OnDelete(DeleteBehavior.SetNull);
    }
}
