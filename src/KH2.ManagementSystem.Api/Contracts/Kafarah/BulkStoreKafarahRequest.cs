namespace KH2.ManagementSystem.Api.Contracts.Kafarah;

public sealed record BulkStoreKafarahRequest(
    DateOnly Tanggal,
    string JenisPelanggaran,
    IReadOnlyCollection<Guid> SantriIds,
    int? JumlahSetor,
    int? Tanggungan,
    string? Tenggat);
