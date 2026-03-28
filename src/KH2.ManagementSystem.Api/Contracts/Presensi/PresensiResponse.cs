namespace KH2.ManagementSystem.Api.Contracts.Presensi;

public sealed record PresensiResponse(
    Guid Id,
    Guid SantriId,
    string SantriNama,
    string SantriNis,
    string SantriTim,
    DateOnly Tanggal,
    string Status,
    string Waktu,
    string Kegiatan,
    string? Keterangan,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record PresensiListResponse(
    IReadOnlyList<PresensiResponse> Items,
    int Page,
    int PerPage,
    int TotalCount);
