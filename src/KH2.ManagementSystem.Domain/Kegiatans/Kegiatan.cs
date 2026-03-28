using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.Kegiatans;

public sealed class Kegiatan : AuditableEntity<Guid>
{
    public static readonly string[] Categories = ["asrama", "sambung", "keakraban"];
    public static readonly string[] Times = ["subuh", "pagi", "siang", "sore", "malam"];

    public Kegiatan(
        Guid id,
        string kategori,
        string waktu,
        string? catatan = null)
        : base(id)
    {
        Kategori = Require(kategori, nameof(kategori));
        Waktu = Require(waktu, nameof(waktu));
        Catatan = NormalizeOptional(catatan);
    }

    public string Kategori { get; private set; } = string.Empty;
    public string Waktu { get; private set; } = string.Empty;
    public string? Catatan { get; private set; }

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
