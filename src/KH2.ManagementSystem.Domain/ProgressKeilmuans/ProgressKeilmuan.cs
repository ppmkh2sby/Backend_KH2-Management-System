using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.ProgressKeilmuans;

public sealed class ProgressKeilmuan : AuditableEntity<Guid>
{
    public const string LevelQuran = "al-quran";
    public const string LevelHadits = "al-hadits";

    public ProgressKeilmuan(
        Guid id,
        Guid santriId,
        string judul,
        int target,
        int capaian,
        string? satuan,
        string? level,
        string? catatan,
        string? pembimbing,
        DateTimeOffset? terakhirSetorUtc)
        : base(id)
    {
        SantriId = santriId;
        Judul = Require(judul, nameof(judul));
        Target = RequireNonNegative(target, nameof(target));
        Capaian = RequireNonNegative(capaian, nameof(capaian));
        Satuan = NormalizeOptional(satuan);
        Level = NormalizeOptional(level);
        Catatan = NormalizeOptional(catatan);
        Pembimbing = NormalizeOptional(pembimbing);
        TerakhirSetorUtc = terakhirSetorUtc;
    }

    public Guid SantriId { get; private set; }
    public string Judul { get; private set; } = string.Empty;
    public int Target { get; private set; }
    public int Capaian { get; private set; }
    public string? Satuan { get; private set; }
    public string? Level { get; private set; }
    public string? Catatan { get; private set; }
    public string? Pembimbing { get; private set; }
    public DateTimeOffset? TerakhirSetorUtc { get; private set; }

    public int Persentase => Target <= 0 ? 0 : Math.Min(100, (int)Math.Round((double)Capaian * 100 / Target));

    public void Update(
        string judul,
        int target,
        int capaian,
        string? satuan,
        string? level,
        string? catatan,
        string? pembimbing,
        DateTimeOffset? terakhirSetorUtc,
        DateTimeOffset updatedAtUtc)
    {
        Judul = Require(judul, nameof(judul));
        Target = RequireNonNegative(target, nameof(target));
        Capaian = RequireNonNegative(capaian, nameof(capaian));
        Satuan = NormalizeOptional(satuan);
        Level = NormalizeOptional(level);
        Catatan = NormalizeOptional(catatan);
        Pembimbing = NormalizeOptional(pembimbing);
        TerakhirSetorUtc = terakhirSetorUtc;
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

    private static int RequireNonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.");
        }

        return value;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
