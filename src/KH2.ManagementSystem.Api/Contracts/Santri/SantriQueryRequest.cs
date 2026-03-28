namespace KH2.ManagementSystem.Api.Contracts.Santri;

public sealed record SantriQueryRequest
{
    public string? Search { get; init; }
    public string? Gender { get; init; }
    public string? Tim { get; init; }
    public bool OnlyMine { get; init; }
    public int Page { get; init; } = 1;
    public int PerPage { get; init; } = 50;
}
