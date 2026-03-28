namespace KH2.ManagementSystem.Api.Contracts.ProgressKeilmuan;

public sealed record ProgressKeilmuanStaffPageResponse(
    string Category,
    ProgressKeilmuanStaffSummaryResponse Summary,
    IReadOnlyList<ProgressKeilmuanStaffRowResponse> Rows,
    int Page,
    int PerPage,
    int TotalCount);

public sealed record ProgressKeilmuanStaffSummaryResponse(
    int TotalSantri,
    int ActiveSantri,
    int Average,
    int CompletedModules,
    int ModuleCount);

public sealed record ProgressKeilmuanStaffRowResponse(
    Guid SantriId,
    string Code,
    string Nama,
    string Gender,
    string Kelas,
    string Tim,
    int Completed,
    int InProgress,
    int Average,
    DateTimeOffset? UpdatedAtUtc);
