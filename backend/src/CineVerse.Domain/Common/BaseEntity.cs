namespace CineVerse.Domain.Common;

/// <summary>
/// Базовий клас для всіх сутностей домену. Забезпечує уніфікований первинний ключ (UUID)
/// та мітку часу створення запису.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
