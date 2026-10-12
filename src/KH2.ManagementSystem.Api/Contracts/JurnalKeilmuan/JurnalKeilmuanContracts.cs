namespace KH2.ManagementSystem.Api.Contracts.JurnalKeilmuan;

public sealed record SaveJurnalKeilmuanRequest(
    DateOnly Tanggal,
    string SesiSambung,
    string Kelas,
    string NamaDewanGuru,
    decimal JamMengajar,
    bool? AdaMateri,
    string JenisMateri,
    Guid? QuranTargetSurahId,
    int? QuranAyatAwal,
    int? QuranAyatTarget,
    Guid? QuranRealisasiSurahId,
    int? QuranAyatRealisasi,
    string? QuranKeterangan,
    Dictionary<string, string?>? Details);

public sealed record QuranSurahOptionResponse(Guid Id, int Number, string Name, string ArabicName, int VerseCount);

public sealed record JurnalKeilmuanItemResponse(
    Guid Id,
    DateOnly Tanggal,
    string Bulan,
    string SesiSambung,
    string Kelas,
    string NamaDewanGuru,
    decimal JamMengajar,
    bool? AdaMateri,
    string JenisMateri,
    string DibuatOleh,
    Guid? QuranTargetSurahId,
    string? QuranTargetSurah,
    int? QuranAyatAwal,
    int? QuranAyatTarget,
    Guid? QuranRealisasiSurahId,
    string? QuranRealisasiSurah,
    int? QuranAyatRealisasi,
    int? TargetAyat,
    int? CapaianAyat,
    int? PersentaseTarget,
    bool? SesuaiTarget,
    string? QuranKeterangan,
    Dictionary<string, string?> Details,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record LaporanHarianJurnalResponse(
    DateOnly Tanggal,
    int JumlahJurnal,
    int JurnalBertarget,
    int JurnalSesuaiTarget,
    int RataRataPersentase,
    bool SesuaiTargetHarian);

public sealed record LaporanBulananJurnalResponse(
    string Bulan,
    int JumlahJurnal,
    int JurnalBertarget,
    int JurnalSesuaiTarget,
    int JurnalBelumSesuai,
    int RataRataPersentase,
    IReadOnlyList<LaporanHarianJurnalResponse> Harian);

public sealed record JurnalKeilmuanPageResponse(
    string Bulan,
    IReadOnlyList<JurnalKeilmuanItemResponse> Items,
    LaporanBulananJurnalResponse Laporan);
