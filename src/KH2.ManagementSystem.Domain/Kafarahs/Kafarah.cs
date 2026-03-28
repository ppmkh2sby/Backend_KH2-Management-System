using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.Kafarahs;

public sealed class Kafarah : AuditableEntity<Guid>
{
    public static readonly IReadOnlyDictionary<string, KafarahDefinition> Definitions =
        new Dictionary<string, KafarahDefinition>(StringComparer.Ordinal)
        {
            ["tidak_sholat_subuh_di_masjid"] = new("tidak_sholat_subuh_di_masjid", "Tidak sholat subuh di masjid", "Istigfar 250", 250),
            ["tidak_sambung_pagi"] = new("tidak_sambung_pagi", "Tidak sambung pagi", "Istigfar 150", 150),
            ["tidak_sambung_malam"] = new("tidak_sambung_malam", "Tidak sambung malam", "Istigfar 150", 150),
            ["tidak_apel_malam"] = new("tidak_apel_malam", "Tidak apel malam", "Istigfar 250", 250),
            ["tidak_sholat_malam"] = new("tidak_sholat_malam", "Tidak sholat malam", "Istigfar 150", 150),
            ["terlambat_kembali_ke_ppm"] = new("terlambat_kembali_ke_ppm", "Terlambat kembali ke PPM", "Membayar 10K/15K/25K", 10000),
            ["tidak_asrama_sesi_pagi"] = new("tidak_asrama_sesi_pagi", "Tidak asrama sesi pagi", "Istigfar 150", 150),
            ["tidak_asrama_sesi_siang"] = new("tidak_asrama_sesi_siang", "Tidak asrama sesi siang", "Istigfar 150", 150),
            ["tidak_asrama_sesi_sore"] = new("tidak_asrama_sesi_sore", "Tidak asrama sesi sore", "Istigfar 150", 150),
            ["tidak_asrama_sesi_malam"] = new("tidak_asrama_sesi_malam", "Tidak asrama sesi malam", "Istigfar 150", 150),
        };

    public Kafarah(
        Guid id,
        Guid santriId,
        DateOnly tanggal,
        string jenisPelanggaran,
        string kafarahText,
        int jumlahSetor,
        int tanggungan,
        string? tenggat = null)
        : base(id)
    {
        SantriId = santriId;
        Tanggal = tanggal;
        JenisPelanggaran = Require(jenisPelanggaran, nameof(jenisPelanggaran));
        KafarahText = Require(kafarahText, nameof(kafarahText));
        JumlahSetor = RequireNonNegative(jumlahSetor, nameof(jumlahSetor));
        Tanggungan = RequireNonNegative(tanggungan, nameof(tanggungan));
        Tenggat = NormalizeOptional(tenggat);
    }

    public Guid SantriId { get; private set; }
    public DateOnly Tanggal { get; private set; }
    public string JenisPelanggaran { get; private set; } = string.Empty;
    public string KafarahText { get; private set; } = string.Empty;
    public int JumlahSetor { get; private set; }
    public int Tanggungan { get; private set; }
    public string? Tenggat { get; private set; }

    public int SisaTanggungan => Math.Max(0, Tanggungan - JumlahSetor);

    public void Update(
        DateOnly tanggal,
        string jenisPelanggaran,
        string kafarahText,
        int jumlahSetor,
        int tanggungan,
        string? tenggat,
        DateTimeOffset updatedAtUtc)
    {
        Tanggal = tanggal;
        JenisPelanggaran = Require(jenisPelanggaran, nameof(jenisPelanggaran));
        KafarahText = Require(kafarahText, nameof(kafarahText));
        JumlahSetor = RequireNonNegative(jumlahSetor, nameof(jumlahSetor));
        Tanggungan = RequireNonNegative(tanggungan, nameof(tanggungan));
        Tenggat = NormalizeOptional(tenggat);
        Touch(updatedAtUtc);
    }

    public static KafarahDefinition ResolveDefinition(string jenisPelanggaran)
    {
        if (Definitions.TryGetValue(jenisPelanggaran, out var definition))
        {
            return definition;
        }

        return new KafarahDefinition(jenisPelanggaran, jenisPelanggaran, "Istigfar 150", 150);
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
