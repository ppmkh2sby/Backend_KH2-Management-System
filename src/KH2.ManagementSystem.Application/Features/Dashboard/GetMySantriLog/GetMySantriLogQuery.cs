using KH2.ManagementSystem.Application.Abstractions.Messaging;
using KH2.ManagementSystem.BuildingBlocks.Results;

namespace KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriLog;

public sealed record GetMySantriLogQuery(Guid UserId) : IQuery<Result<SantriDashboardLogPageDto>>;
