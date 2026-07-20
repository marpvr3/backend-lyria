namespace Lyria.Domain.Abstractions;

public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; }
    DateTime? UpdatedAtUtc { get; }
    void SetCreatedAtUtc(DateTime value);
    void SetUpdatedAtUtc(DateTime value);
}
