using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriDashboard;

namespace KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriProgress;

public sealed record SantriDashboardProgressPageDto(
    DateTimeOffset GeneratedAtUtc,
    SantriDashboardProfileDto Profile,
    SantriDashboardProgressPageSummaryDto Summary,
    IReadOnlyList<SantriDashboardProgressItemDto> RecentUpdates,
    IReadOnlyList<SantriDashboardProgressItemDto> Items);

public sealed record SantriDashboardProgressPageSummaryDto(
    int Total,
    int Completed,
    int InProgress,
    int Average);
