using KH2.ManagementSystem.Application.Abstractions.Dashboard;
using KH2.ManagementSystem.Application.Abstractions.Messaging;
using KH2.ManagementSystem.BuildingBlocks.Results;

namespace KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriAttendance;

public sealed class GetMySantriAttendanceQueryHandler(ISantriDashboardReader reader)
    : IQueryHandler<GetMySantriAttendanceQuery, Result<SantriDashboardAttendancePageDto>>
{
    private static readonly AppError SantriAttendanceUnavailable = new(
        "dashboard.santri_attendance_unavailable",
        "Santri attendance dashboard is unavailable for the current user.");

    public async Task<Result<SantriDashboardAttendancePageDto>> HandleAsync(
        GetMySantriAttendanceQuery query,
        CancellationToken cancellationToken = default)
    {
        var dashboard = await reader.GetAttendanceByUserIdAsync(query.UserId, cancellationToken);

        if (dashboard is null)
        {
            return Result.Failure<SantriDashboardAttendancePageDto>(SantriAttendanceUnavailable);
        }

        return Result.Success(dashboard);
    }
}
