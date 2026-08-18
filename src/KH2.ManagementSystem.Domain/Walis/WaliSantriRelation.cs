using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.Walis;

public sealed class WaliSantriRelation : AuditableEntity<Guid>
{
    public WaliSantriRelation(
        Guid id,
        Guid waliUserId,
        Guid santriId,
        string relationshipLabel,
        string waliSantriCode)
        : base(id)
    {
        WaliUserId = waliUserId;
        SantriId = santriId;
        ChangeRelationshipLabel(relationshipLabel);
        ChangeWaliSantriCode(waliSantriCode);
    }

    public Guid WaliUserId { get; private set; }
    public Guid SantriId { get; private set; }
    public string RelationshipLabel { get; private set; } = string.Empty;
    public string WaliSantriCode { get; private set; } = string.Empty;

    public void ChangeRelationshipLabel(string relationshipLabel)
    {
        if (string.IsNullOrWhiteSpace(relationshipLabel))
        {
            throw new ArgumentException("Relationship label is required.", nameof(relationshipLabel));
        }

        RelationshipLabel = relationshipLabel.Trim();
        Touch(DateTimeOffset.UtcNow);
    }

    public void ChangeWaliSantriCode(string waliSantriCode)
    {
        if (string.IsNullOrWhiteSpace(waliSantriCode))
        {
            throw new ArgumentException("Wali santri code is required.", nameof(waliSantriCode));
        }

        WaliSantriCode = waliSantriCode.Trim();
        Touch(DateTimeOffset.UtcNow);
    }
}
