using KH2.ManagementSystem.Application.Abstractions.Dashboard;
using KH2.ManagementSystem.Application.Abstractions.Messaging;
using KH2.ManagementSystem.BuildingBlocks.Results;

namespace KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriDashboard;

public sealed class GetMySantriDashboardQueryHandler(ISantriDashboardReader reader)
    : IQueryHandler<GetMySantriDashboardQuery, Result<SantriDashboardDto>>
{
    private static readonly AppError SantriDashboardUnavailable = new(
        "dashboard.santri_unavailable",
        "Santri dashboard is unavailable for the current user.");

    public async Task<Result<SantriDashboardDto>> HandleAsync(
        GetMySantriDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        var dashboard = await reader.GetOverviewByUserIdAsync(query.UserId, cancellationToken);

        if (dashboard is null)
        {
            return Result.Failure<SantriDashboardDto>(SantriDashboardUnavailable);
        }

        return Result.Success(dashboard);
    }
}
