namespace KH2.ManagementSystem.Api.Contracts.Presensi;

public sealed record PresensiRecapQueryRequest
{
    public string? Bulan { get; init; }
    public string? Gender { get; init; }
    public string? Kategori { get; init; }
    public string? Waktu { get; init; }
    public int Page { get; init; } = 1;
    public int PerPage { get; init; } = 8;
}
