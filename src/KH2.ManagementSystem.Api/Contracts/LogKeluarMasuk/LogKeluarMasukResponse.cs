namespace KH2.ManagementSystem.Api.Contracts.LogKeluarMasuk;

public sealed record LogKeluarMasukResponse(
    Guid Id,
    Guid SantriId,
    string SantriNama,
    string SantriNis,
    string SantriTim,
    string SantriGender,
    DateOnly Tanggal,
    string Tujuan,
    string? WaktuKeluar,
    string? WaktuMasuk,
    string? Rentang,
    string Status,
    string? Catatan,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record LogKeluarMasukListResponse(
    IReadOnlyList<LogKeluarMasukResponse> Items,
    int Page,
    int PerPage,
    int TotalCount);
