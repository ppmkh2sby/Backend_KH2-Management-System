using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.Presensis;

public sealed class Presensi : AuditableEntity<Guid>
{
    public static readonly string[] Statuses = ["hadir", "izin", "alpa", "sakit"];
    public static readonly string[] Times = ["subuh", "pagi", "siang", "sore", "malam"];

    public Presensi(
        Guid id,
        Guid santriId,
        string nama,
        string status,
        Guid kegiatanId,
        Guid? sesiId,
        string? catatan,
        string waktu)
        : base(id)
    {
        SantriId = santriId;
        Nama = Require(nama, nameof(nama));
        Status = Require(status, nameof(status));
        KegiatanId = kegiatanId;
        SesiId = sesiId;
        Catatan = NormalizeOptional(catatan);
        Waktu = Require(waktu, nameof(waktu));
    }

    public Guid SantriId { get; private set; }
    public string Nama { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public Guid KegiatanId { get; private set; }
    public Guid? SesiId { get; private set; }
    public string? Catatan { get; private set; }
    public string Waktu { get; private set; } = string.Empty;

    public void Update(
        Guid santriId,
        string nama,
        string status,
        Guid kegiatanId,
        Guid? sesiId,
        string? catatan,
        string waktu,
        DateTimeOffset updatedAtUtc)
    {
        SantriId = santriId;
        Nama = Require(nama, nameof(nama));
        Status = Require(status, nameof(status));
        KegiatanId = kegiatanId;
        SesiId = sesiId;
        Catatan = NormalizeOptional(catatan);
        Waktu = Require(waktu, nameof(waktu));
        Touch(updatedAtUtc);
    }

    private static string Require(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} is required.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
