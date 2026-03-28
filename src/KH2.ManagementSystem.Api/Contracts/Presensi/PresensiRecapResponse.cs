namespace KH2.ManagementSystem.Api.Contracts.Presensi;

public sealed record PresensiRecapResponse(
    string Bulan,
    string? Gender,
    string? Kategori,
    string? Waktu,
    PresensiRecapSummaryResponse Summary,
    IReadOnlyList<PresensiRecapItemResponse> Items,
    int Page,
    int PerPage,
    int TotalCount);

public sealed record PresensiRecapSummaryResponse(
    int TotalSantri,
    int TotalSesi,
    int TotalInput,
    int Hadir,
    int Izin,
    int Sakit,
    int Alpa,
    int Persentase);

public sealed record PresensiRecapItemResponse(
    Guid SantriId,
    string Nama,
    string Nis,
    string Tim,
    string Gender,
    int TotalInput,
    int Hadir,
    int Izin,
    int Sakit,
    int Alpa,
    int Persentase);
