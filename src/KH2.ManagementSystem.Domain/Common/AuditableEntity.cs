namespace KH2.ManagementSystem.Domain.Common;

public abstract class AuditableEntity<TId> : Entity<TId>
    where TId : notnull
{
    protected AuditableEntity(TId id)
        : base(id)
    {
    }

    public DateTimeOffset CreatedAtUtc { get; protected set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAtUtc { get; protected set; }

    protected void Touch(DateTimeOffset timestamp)
    {
        UpdatedAtUtc = timestamp;
    }
}
