using CineVerse.Domain.Common;
using CineVerse.Domain.Enums;

namespace CineVerse.Domain.Entities;

/// <summary>Оплата, пов'язана з бронюванням.</summary>
public class Payment : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.ONLINE;
    public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;
    public string? TransactionId { get; set; }
}
