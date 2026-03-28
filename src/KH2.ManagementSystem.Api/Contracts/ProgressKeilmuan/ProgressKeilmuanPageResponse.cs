namespace KH2.ManagementSystem.Api.Contracts.ProgressKeilmuan;

public sealed record ProgressKeilmuanPageResponse(
    string Category,
    ProgressKeilmuanSummaryResponse Summary,
    IReadOnlyList<ProgressKeilmuanModuleResponse> Modules,
    IReadOnlyList<ProgressKeilmuanRecentResponse> RecentUpdates);

public sealed record ProgressKeilmuanSummaryResponse(
    int Total,
    int Completed,
    int InProgress,
    int Average);

public sealed record ProgressKeilmuanModuleResponse(
    string Judul,
    int Target,
    int? Value,
    int Persentase,
    DateTimeOffset? UpdatedAtUtc);

public sealed record ProgressKeilmuanRecentResponse(
    Guid Id,
    string Judul,
    int Capaian,
    int Target,
    string? Satuan,
    int Persentase,
    DateTimeOffset? TerakhirSetorUtc,
    DateTimeOffset? UpdatedAtUtc);
