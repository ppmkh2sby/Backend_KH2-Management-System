using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.Sesis;

public sealed class Sesi : AuditableEntity<Guid>
{
    public Sesi(
        Guid id,
        Guid kegiatanId,
        DateOnly tanggal,
        string? catatan = null)
        : base(id)
    {
        KegiatanId = kegiatanId;
        Tanggal = tanggal;
        Catatan = NormalizeOptional(catatan);
    }

    public Guid KegiatanId { get; private set; }
    public DateOnly Tanggal { get; private set; }
    public string? Catatan { get; private set; }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
