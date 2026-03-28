namespace KH2.ManagementSystem.Api.Contracts.Kafarah;

public sealed record KafarahQueryRequest
{
    public Guid? SantriId { get; init; }
    public string? Search { get; init; }
    public string? Gender { get; init; }
    public string? Tim { get; init; }
    public int Page { get; init; } = 1;
    public int PerPage { get; init; } = 20;
}
