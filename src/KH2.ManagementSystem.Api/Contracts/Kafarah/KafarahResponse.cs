namespace KH2.ManagementSystem.Api.Contracts.Kafarah;

public sealed record KafarahResponse(
    Guid Id,
    Guid SantriId,
    string SantriNama,
    string SantriNis,
    string SantriTim,
    string SantriGender,
    DateOnly Tanggal,
    string JenisPelanggaran,
    string JenisPelanggaranLabel,
    string Kafarah,
    int JumlahSetor,
    int Tanggungan,
    int SisaTanggungan,
    string? Tenggat,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record KafarahListResponse(
    IReadOnlyList<KafarahResponse> Items,
    int Page,
    int PerPage,
    int TotalCount);

public sealed record BulkKafarahResponse(
    IReadOnlyList<KafarahResponse> Items,
    int CreatedCount,
    int UpdatedCount);
