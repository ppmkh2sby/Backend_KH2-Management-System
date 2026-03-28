namespace KH2.ManagementSystem.Api.Contracts.Presensi;

public sealed class PresensiQueryRequest
{
    public Guid? SantriId { get; init; }
    public string? Status { get; init; }
    public bool OnlyMine { get; init; }
    public DateOnly? TanggalDari { get; init; }
    public DateOnly? TanggalSampai { get; init; }
    public int Page { get; init; } = 1;
    public int PerPage { get; init; } = 15;
}
