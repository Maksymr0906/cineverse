namespace CineVerse.Application.Features.Tickets.Common;

public record TicketDto(
    Guid Id,
    int RowNumber,
    int SeatNumber,
    string TicketCode,
    string QrCodeBase64,
    string Status
);
