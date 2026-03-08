namespace KH2.ManagementSystem.Api.Options;

public sealed class ApplicationMetadataOptions
{
    public const string SectionName = "ApplicationMetadata";

    public string Name { get; init; } = "KH2 Management System API";

    public string Version { get; init; } = "v1";
}
