using KH2.ManagementSystem.Domain.FaceRecognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class LegacyFaceEnrollmentConfiguration : IEntityTypeConfiguration<LegacyFaceEnrollment>
{
    public void Configure(EntityTypeBuilder<LegacyFaceEnrollment> builder)
    {
        builder.ToTable("LegacyFaceEnrollments"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired(); builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.RejectionReason).HasMaxLength(500); builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasOne<Domain.Users.User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
