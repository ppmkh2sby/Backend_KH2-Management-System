using Microsoft.AspNetCore.Mvc;

namespace KH2.ManagementSystem.Api.Contracts.ProgressKeilmuan;

public sealed record ProgressKeilmuanQueryRequest
{
    [FromQuery(Name = "category")]
    public string? Category { get; init; }

    [FromQuery(Name = "gender")]
    public string? Gender { get; init; }

    [FromQuery(Name = "q")]
    public string? Search { get; init; }

    [FromQuery(Name = "page")]
    public int Page { get; init; } = 1;

    [FromQuery(Name = "perPage")]
    public int PerPage { get; init; } = 8;
}
