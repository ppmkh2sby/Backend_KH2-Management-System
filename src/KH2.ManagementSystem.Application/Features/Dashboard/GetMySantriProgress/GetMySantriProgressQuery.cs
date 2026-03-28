using KH2.ManagementSystem.Application.Abstractions.Messaging;
using KH2.ManagementSystem.BuildingBlocks.Results;

namespace KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriProgress;

public sealed record GetMySantriProgressQuery(Guid UserId) : IQuery<Result<SantriDashboardProgressPageDto>>;
