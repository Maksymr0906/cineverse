namespace CineVerse.Application.Features.Bookings.Common;

public record SeatMapSeatDto(Guid SeatId, int RowNumber, int SeatNumber, string SeatType, string Status, decimal Price);

public record SeatMapDto(Guid SessionId, Guid HallId, string HallName, IReadOnlyList<SeatMapSeatDto> Seats);

public record BookingSeatResultDto(Guid SeatId, int RowNumber, int SeatNumber, decimal Price);

/// <summary>
/// Виправлення після аудиту: раніше DTO не містив жодної інформації про сам сеанс/фільм —
/// сторінка "Мої бронювання" могла показати лише координати місць ("3:5, 3:6") і статус/суму,
/// що не дозволяло користувачу відрізнити одне бронювання від іншого за змістом.
/// </summary>
public record BookingDto(
    Guid Id,
    Guid SessionId,
    string Status,
    decimal TotalPrice,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<BookingSeatResultDto> Seats,
    string MovieTitle,
    string PosterUrl,
    string HallName,
    string CinemaName,
    DateTimeOffset SessionStartTime,
    string? AppliedPromoCode = null,
    decimal DiscountAmount = 0m
);
