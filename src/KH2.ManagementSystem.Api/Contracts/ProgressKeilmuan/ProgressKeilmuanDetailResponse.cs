namespace KH2.ManagementSystem.Api.Contracts.ProgressKeilmuan;

public sealed record ProgressKeilmuanDetailResponse(
    ProgressKeilmuanDetailSantriResponse Santri,
    ProgressKeilmuanDetailSectionResponse Quran,
    ProgressKeilmuanDetailSectionResponse Hadits);

public sealed record ProgressKeilmuanDetailSantriResponse(
    Guid Id,
    string Nama,
    string Nis,
    string Gender,
    string Tim,
    string Kelas);

public sealed record ProgressKeilmuanDetailSectionResponse(
    string Category,
    ProgressKeilmuanSummaryResponse Summary,
    IReadOnlyList<ProgressKeilmuanModuleResponse> Modules,
    IReadOnlyList<ProgressKeilmuanRecentResponse> RecentUpdates);
