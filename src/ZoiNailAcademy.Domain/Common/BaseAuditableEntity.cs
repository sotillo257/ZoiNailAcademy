namespace ZoiNailAcademy.Domain.Common;

public abstract class BaseAuditableEntity
{
    public Guid Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
