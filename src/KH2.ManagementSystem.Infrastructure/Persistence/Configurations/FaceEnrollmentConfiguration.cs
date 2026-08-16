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
        builder.Property(x => x.SantriId).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.CaptureCount).IsRequired();
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.HasIndex(x => x.SantriId).IsUnique();
        builder.HasOne<Domain.Santris.Santri>().WithMany().HasForeignKey(x => x.SantriId).OnDelete(DeleteBehavior.Cascade);
    }
}
