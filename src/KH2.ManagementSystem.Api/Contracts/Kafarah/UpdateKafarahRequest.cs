namespace KH2.ManagementSystem.Api.Contracts.Kafarah;

public sealed record UpdateKafarahRequest
{
    public DateOnly? Tanggal { get; init; }
    public string? JenisPelanggaran { get; init; }
    public int? JumlahSetor { get; init; }
    public int? Tanggungan { get; init; }
    public string? Tenggat { get; init; }
}
