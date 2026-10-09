using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.FaceRecognition;

public sealed class FaceAttendanceSession : AuditableEntity<Guid>
{
    public FaceAttendanceSession(Guid id, string kelas, string kegiatan, string waktu, DateOnly tanggal, Guid openerUserId, Guid kegiatanId, Guid sesiId)
        : base(id)
    {
        Kelas = Require(kelas, nameof(kelas));
        Kegiatan = Require(kegiatan, nameof(kegiatan));
        Waktu = Require(waktu, nameof(waktu));
        Tanggal = tanggal;
        OpenerUserId = openerUserId;
        KegiatanId = kegiatanId;
        SesiId = sesiId;
        Status = FaceAttendanceSessionStatus.AwaitingVerification;
    }

    public string Kelas { get; private set; } = string.Empty;
    public string Kegiatan { get; private set; } = string.Empty;
    public string Waktu { get; private set; } = string.Empty;
    public DateOnly Tanggal { get; private set; }
    public Guid OpenerUserId { get; private set; }
    public Guid KegiatanId { get; private set; }
    public Guid SesiId { get; private set; }
    public FaceAttendanceSessionStatus Status { get; private set; }
    public DateTimeOffset? VerifiedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public void Open(DateTimeOffset now)
    {
        if (Status is FaceAttendanceSessionStatus.Closed)
        {
            throw new InvalidOperationException("Sesi sudah ditutup.");
        }

        Status = FaceAttendanceSessionStatus.Open;
        VerifiedAtUtc = now;
        Touch(now);
    }

    public void Close(DateTimeOffset now)
    {
        if (Status is not FaceAttendanceSessionStatus.Open)
        {
            throw new InvalidOperationException("Hanya sesi terbuka yang dapat ditutup.");
        }

        Status = FaceAttendanceSessionStatus.Closed;
        ClosedAtUtc = now;
        Touch(now);
    }

    private static string Require(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{name} is required.", name) : value.Trim();
}
