using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriDashboard;

namespace KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriLog;

public sealed record SantriDashboardLogPageDto(
    DateTimeOffset GeneratedAtUtc,
    SantriDashboardProfileDto Profile,
    SantriDashboardLogPageSummaryDto Summary,
    IReadOnlyList<SantriDashboardLogItemDto> Timeline,
    IReadOnlyList<SantriDashboardLogItemDto> Logs);

public sealed record SantriDashboardLogPageSummaryDto(
    int Total,
    int Disetujui,
    int Proses,
    int Tercatat,
    int Ditolak);
