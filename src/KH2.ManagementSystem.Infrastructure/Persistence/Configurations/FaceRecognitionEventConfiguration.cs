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
        builder.Property(x => x.Confidence).HasPrecision(5, 4);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.HasIndex(x => new { x.SessionId, x.CapturedAtUtc });
        builder.HasOne<FaceAttendanceSession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Domain.Santris.Santri>().WithMany().HasForeignKey(x => x.SantriId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Domain.Presensis.Presensi>().WithMany().HasForeignKey(x => x.PresensiId).OnDelete(DeleteBehavior.SetNull);
    }
}
