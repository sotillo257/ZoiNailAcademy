namespace ZoiNailAcademy.Domain.Common;

public abstract class BaseAuditableEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
