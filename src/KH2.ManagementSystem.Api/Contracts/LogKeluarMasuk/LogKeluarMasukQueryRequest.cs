namespace KH2.ManagementSystem.Api.Contracts.LogKeluarMasuk;

public sealed record LogKeluarMasukQueryRequest
{
    public int Page { get; init; } = 1;
    public int PerPage { get; init; } = 12;
    public string? Search { get; init; }
    public string? Gender { get; init; }
    public string? Scope { get; init; }
}
