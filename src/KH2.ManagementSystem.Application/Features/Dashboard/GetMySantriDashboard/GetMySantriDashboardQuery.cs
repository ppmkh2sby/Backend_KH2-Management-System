using KH2.ManagementSystem.Application.Abstractions.Messaging;
using KH2.ManagementSystem.BuildingBlocks.Results;

namespace KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriDashboard;

public sealed record GetMySantriDashboardQuery(Guid UserId) : IQuery<Result<SantriDashboardDto>>;
