namespace KH2.ManagementSystem.Api.Contracts;

public sealed record SystemInfoResponse(
    string Name,
    string Version,
    string Environment,
    DateTimeOffset ServerTimeUtc);
