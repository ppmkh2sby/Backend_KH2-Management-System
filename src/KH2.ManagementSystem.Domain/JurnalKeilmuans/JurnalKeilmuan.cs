using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.JurnalKeilmuans;

public sealed class JurnalKeilmuan : AuditableEntity<Guid>
{
    private JurnalKeilmuan() : base(Guid.Empty) { }

    public JurnalKeilmuan(Guid id, JurnalKeilmuanData data) : base(id) => Apply(data, null);

    public DateOnly Tanggal { get; private set; }
    public string SesiSambung { get; private set; } = string.Empty;
    public string Kelas { get; private set; } = string.Empty;
    public string NamaDewanGuru { get; private set; } = string.Empty;
    public decimal JamMengajar { get; private set; }
    public bool? AdaMateri { get; private set; }
    public string JenisMateri { get; private set; } = string.Empty;
    public Guid DibuatOlehUserId { get; private set; }
    public string DibuatOleh { get; private set; } = string.Empty;
    public Guid? QuranTargetSurahId { get; private set; }
    public int? QuranAyatAwal { get; private set; }
    public int? QuranAyatTarget { get; private set; }
    public Guid? QuranRealisasiSurahId { get; private set; }
    public int? QuranAyatRealisasi { get; private set; }
    public string? QuranKeterangan { get; private set; }
    public string DetailsJson { get; private set; } = "{}";

    public void Update(JurnalKeilmuanData data, DateTimeOffset updatedAtUtc) => Apply(data, updatedAtUtc);

    private void Apply(JurnalKeilmuanData data, DateTimeOffset? updatedAtUtc)
    {
        Tanggal = data.Tanggal;
        SesiSambung = Required(data.SesiSambung, nameof(data.SesiSambung));
        Kelas = Required(data.Kelas, nameof(data.Kelas));
        NamaDewanGuru = Required(data.NamaDewanGuru, nameof(data.NamaDewanGuru));
        JamMengajar = data.JamMengajar is >= 0m and <= 24m ? data.JamMengajar : throw new ArgumentOutOfRangeException(nameof(data));
        AdaMateri = data.AdaMateri;
        JenisMateri = Required(data.JenisMateri, nameof(data.JenisMateri));
        DibuatOlehUserId = data.DibuatOlehUserId;
        DibuatOleh = Required(data.DibuatOleh, nameof(data.DibuatOleh));
        QuranTargetSurahId = data.QuranTargetSurahId;
        QuranAyatAwal = data.QuranAyatAwal;
        QuranAyatTarget = data.QuranAyatTarget;
        QuranRealisasiSurahId = data.QuranRealisasiSurahId;
        QuranAyatRealisasi = data.QuranAyatRealisasi;
        QuranKeterangan = Optional(data.QuranKeterangan);
        DetailsJson = string.IsNullOrWhiteSpace(data.DetailsJson) ? "{}" : data.DetailsJson;
        if (updatedAtUtc.HasValue) Touch(updatedAtUtc.Value);
    }

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", parameterName) : value.Trim();

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record JurnalKeilmuanData(
    DateOnly Tanggal,
    string SesiSambung,
    string Kelas,
    string NamaDewanGuru,
    decimal JamMengajar,
    bool? AdaMateri,
    string JenisMateri,
    Guid DibuatOlehUserId,
    string DibuatOleh,
    Guid? QuranTargetSurahId,
    int? QuranAyatAwal,
    int? QuranAyatTarget,
    Guid? QuranRealisasiSurahId,
    int? QuranAyatRealisasi,
    string? QuranKeterangan,
    string DetailsJson);
