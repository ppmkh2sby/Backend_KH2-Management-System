namespace KH2.ManagementSystem.Infrastructure.Authorization;

public sealed class DevelopmentAuthorizationOptions
{
    public const string SectionName = "DevelopmentAuthorization";

    public bool Enabled { get; init; }
    public List<DevelopmentSantriOwnershipOptions> SantriOwnerships { get; init; } = [];
    public List<DevelopmentWaliSantriRelationOptions> WaliSantriRelations { get; init; } = [];
}

public sealed class DevelopmentSantriOwnershipOptions
{
    public Guid UserId { get; init; }
    public Guid SantriId { get; init; }
}

public sealed class DevelopmentWaliSantriRelationOptions
{
    public Guid WaliUserId { get; init; }
    public List<Guid> SantriIds { get; init; } = [];
}