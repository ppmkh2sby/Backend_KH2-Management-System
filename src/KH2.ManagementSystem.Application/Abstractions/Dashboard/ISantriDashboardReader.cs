using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriAttendance;
using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriDashboard;
using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriLog;
using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriProgress;

namespace KH2.ManagementSystem.Application.Abstractions.Dashboard;

public interface ISantriDashboardReader
{
    Task<SantriDashboardDto?> GetOverviewByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<SantriDashboardAttendancePageDto?> GetAttendanceByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<SantriDashboardProgressPageDto?> GetProgressByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<SantriDashboardLogPageDto?> GetLogByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
