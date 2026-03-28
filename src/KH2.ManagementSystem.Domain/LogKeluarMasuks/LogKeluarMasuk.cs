using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.LogKeluarMasuks;

public sealed class LogKeluarMasuk : AuditableEntity<Guid>
{
    public const string StatusApproved = "disetujui";
    public const string StatusPending = "proses";
    public const string StatusRecorded = "tercatat";
    public const string StatusRejected = "ditolak";

    public static readonly string[] Statuses =
        [StatusApproved, StatusPending, StatusRecorded, StatusRejected];

    public LogKeluarMasuk(
        Guid id,
        Guid santriId,
        DateOnly tanggalPengajuan,
        string jenis,
        string? rentang,
        string status,
        string? petugas,
        string? catatan = null)
        : base(id)
    {
        SantriId = santriId;
        TanggalPengajuan = tanggalPengajuan;
        Jenis = Require(jenis, nameof(jenis));
        Rentang = NormalizeOptional(rentang);
        Status = Require(status, nameof(status));
        Petugas = NormalizeOptional(petugas);
        Catatan = NormalizeOptional(catatan);
    }

    public Guid SantriId { get; private set; }
    public DateOnly TanggalPengajuan { get; private set; }
    public string Jenis { get; private set; } = string.Empty;
    public string? Rentang { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public string? Petugas { get; private set; }
    public string? Catatan { get; private set; }

    public void Update(
        DateOnly tanggalPengajuan,
        string jenis,
        string? rentang,
        string status,
        string? petugas,
        string? catatan,
        DateTimeOffset updatedAtUtc)
    {
        TanggalPengajuan = tanggalPengajuan;
        Jenis = Require(jenis, nameof(jenis));
        Rentang = NormalizeOptional(rentang);
        Status = Require(status, nameof(status));
        Petugas = NormalizeOptional(petugas);
        Catatan = NormalizeOptional(catatan);
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
