using KH2.ManagementSystem.Application.Abstractions.Messaging;
using KH2.ManagementSystem.BuildingBlocks.Results;

namespace KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriAttendance;

public sealed record GetMySantriAttendanceQuery(Guid UserId) : IQuery<Result<SantriDashboardAttendancePageDto>>;
