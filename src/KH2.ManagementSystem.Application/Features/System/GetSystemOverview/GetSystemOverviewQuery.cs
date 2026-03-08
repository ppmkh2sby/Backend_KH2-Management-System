using KH2.ManagementSystem.Application.Abstractions.Messaging;
using KH2.ManagementSystem.BuildingBlocks.Results;

namespace KH2.ManagementSystem.Application.Features.System.GetSystemOverview;

public sealed record GetSystemOverviewQuery : IQuery<Result<SystemOverviewDto>>;
