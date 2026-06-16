namespace ChristianJewelry.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public abstract class AuditableEntity : BaseEntity
{
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}