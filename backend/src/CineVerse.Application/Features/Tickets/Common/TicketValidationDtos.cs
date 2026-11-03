namespace CineVerse.Application.Features.Tickets.Common;

/// <summary>Деталі квитка для показу контролеру ПЕРЕД підтвердженням (крок 1 сканування: превʼю).</summary>
public record TicketPreviewDto(
    Guid TicketId,
    string TicketCode,
    string Status,
    string MovieTitle,
    string HallName,
    DateTimeOffset SessionStartTime,
    int RowNumber,
    int SeatNumber,
    string HolderFullName,
    DateTimeOffset? ValidatedAt
);

/// <summary>Результат підтвердження проходу (крок 2: після натискання "Пропустити" в PWA).</summary>
public record TicketValidationResultDto(Guid TicketId, string TicketCode, DateTimeOffset ValidatedAt, string SeatLabel);
