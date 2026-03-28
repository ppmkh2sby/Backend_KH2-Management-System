using KH2.ManagementSystem.Application.Abstractions.Dashboard;
using KH2.ManagementSystem.Application.Abstractions.Messaging;
using KH2.ManagementSystem.BuildingBlocks.Results;

namespace KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriLog;

public sealed class GetMySantriLogQueryHandler(ISantriDashboardReader reader)
    : IQueryHandler<GetMySantriLogQuery, Result<SantriDashboardLogPageDto>>
{
    private static readonly AppError SantriLogUnavailable = new(
        "dashboard.santri_log_unavailable",
        "Santri log dashboard is unavailable for the current user.");

    public async Task<Result<SantriDashboardLogPageDto>> HandleAsync(
        GetMySantriLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var dashboard = await reader.GetLogByUserIdAsync(query.UserId, cancellationToken);

        if (dashboard is null)
        {
            return Result.Failure<SantriDashboardLogPageDto>(SantriLogUnavailable);
        }

        return Result.Success(dashboard);
    }
}
