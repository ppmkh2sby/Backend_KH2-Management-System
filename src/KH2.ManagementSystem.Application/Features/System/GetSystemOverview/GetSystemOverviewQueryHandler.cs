using KH2.ManagementSystem.Application.Abstractions.Messaging;
using KH2.ManagementSystem.Application.Abstractions.Time;
using KH2.ManagementSystem.BuildingBlocks.Results;

namespace KH2.ManagementSystem.Application.Features.System.GetSystemOverview;

public sealed class GetSystemOverviewQueryHandler(IClock clock)
    : IQueryHandler<GetSystemOverviewQuery, Result<SystemOverviewDto>>
{
    public Task<Result<SystemOverviewDto>> HandleAsync(
        GetSystemOverviewQuery query,
        CancellationToken cancellationToken = default)
    {
        var overview = new SystemOverviewDto(clock.UtcNow);

        return Task.FromResult(Result.Success(overview));
    }
}
