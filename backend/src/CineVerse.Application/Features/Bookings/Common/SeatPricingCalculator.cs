using CineVerse.Domain.Enums;

namespace CineVerse.Application.Features.Bookings.Common;

/// <summary>
/// Розрахунок ціни місця відносно базової ціни сеансу залежно від типу крісла (п.2.1 ТЗ:
/// "динамічне ціноутворення залежно від типу місця"). Love Seat розрахований як "місце на двох".
/// </summary>
public static class SeatPricingCalculator
{
    public static decimal CalculatePrice(decimal basePrice, SeatType seatType) => seatType switch
    {
        SeatType.STANDARD => basePrice,
        SeatType.DISABLED => basePrice,
        SeatType.VIP => Math.Round(basePrice * 1.3m, 2),
        SeatType.LOVE_SEAT => Math.Round(basePrice * 1.8m, 2),
        _ => basePrice
    };
}
