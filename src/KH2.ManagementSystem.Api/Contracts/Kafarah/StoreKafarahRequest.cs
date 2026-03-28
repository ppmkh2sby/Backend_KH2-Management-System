namespace KH2.ManagementSystem.Api.Contracts.Kafarah;

public sealed record StoreKafarahRequest(
    Guid SantriId,
    DateOnly Tanggal,
    string JenisPelanggaran,
    int? JumlahSetor,
    int? Tanggungan,
    string? Tenggat);
