namespace KH2.ManagementSystem.Api.Contracts.Presensi;

public sealed record BulkStorePresensiRequest(
    DateOnly Tanggal,
    string? Kegiatan,
    string? Waktu,
    string? Keterangan,
    IReadOnlyCollection<BulkStorePresensiItemRequest> Items);

public sealed record BulkStorePresensiItemRequest(
    Guid SantriId,
    string Status);

public sealed record BulkPresensiResponse(
    IReadOnlyList<PresensiResponse> Items,
    int CreatedCount,
    int UpdatedCount);
