using CineVerse.Domain.Common;
using CineVerse.Domain.Enums;

namespace CineVerse.Domain.Entities;

/// <summary>Промокод на знижку.</summary>
public class Promotion : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; } = DiscountType.PERCENT;
    public decimal DiscountValue { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public int? UsageLimit { get; set; }
    public int UsageCount { get; set; } = 0;
}
