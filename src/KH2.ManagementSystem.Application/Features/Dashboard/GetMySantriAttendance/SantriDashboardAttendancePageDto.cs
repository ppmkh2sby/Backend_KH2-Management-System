using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriDashboard;

namespace KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriAttendance;

public sealed record SantriDashboardAttendancePageDto(
    DateTimeOffset GeneratedAtUtc,
    SantriDashboardProfileDto Profile,
    bool CanManageAttendance,
    SantriDashboardAttendancePageSummaryDto Summary,
    IReadOnlyList<SantriDashboardAttendanceItemDto> Recent,
    IReadOnlyList<SantriDashboardAttendanceItemDto> Issues,
    IReadOnlyList<SantriDashboardAttendanceItemDto> History);

public sealed record SantriDashboardAttendancePageSummaryDto(
    int Total,
    int Hadir,
    int Izin,
    int Alpa,
    int Sakit,
    int Persentase);
