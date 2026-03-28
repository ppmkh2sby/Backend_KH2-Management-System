using KH2.ManagementSystem.Application.Abstractions.Dashboard;
using KH2.ManagementSystem.Application.Abstractions.Messaging;
using KH2.ManagementSystem.BuildingBlocks.Results;

namespace KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriProgress;

public sealed class GetMySantriProgressQueryHandler(ISantriDashboardReader reader)
    : IQueryHandler<GetMySantriProgressQuery, Result<SantriDashboardProgressPageDto>>
{
    private static readonly AppError SantriProgressUnavailable = new(
        "dashboard.santri_progress_unavailable",
        "Santri progress dashboard is unavailable for the current user.");

    public async Task<Result<SantriDashboardProgressPageDto>> HandleAsync(
        GetMySantriProgressQuery query,
        CancellationToken cancellationToken = default)
    {
        var dashboard = await reader.GetProgressByUserIdAsync(query.UserId, cancellationToken);

        if (dashboard is null)
        {
            return Result.Failure<SantriDashboardProgressPageDto>(SantriProgressUnavailable);
        }

        return Result.Success(dashboard);
    }
}
