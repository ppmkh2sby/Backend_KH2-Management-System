using KH2.ManagementSystem.Domain.FaceRecognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class FaceAttendanceSessionConfiguration : IEntityTypeConfiguration<FaceAttendanceSession>
{
    public void Configure(EntityTypeBuilder<FaceAttendanceSession> builder)
    {
        builder.ToTable("FaceAttendanceSessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Kelas).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Kegiatan).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Waktu).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(x => new { x.Tanggal, x.Status });
        builder.HasIndex(x => x.OpenerUserId);
        builder.HasOne<Domain.Kegiatans.Kegiatan>().WithMany().HasForeignKey(x => x.KegiatanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Domain.Sesis.Sesi>().WithMany().HasForeignKey(x => x.SesiId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Domain.Users.User>().WithMany().HasForeignKey(x => x.OpenerUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
