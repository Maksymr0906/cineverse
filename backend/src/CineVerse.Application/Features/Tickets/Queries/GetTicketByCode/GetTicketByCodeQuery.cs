using CineVerse.Application.Features.Tickets.Common;
using MediatR;

namespace CineVerse.Application.Features.Tickets.Queries.GetTicketByCode;

/// <summary>
/// Крок 1 сканування в кабінеті контролера (PWA, п.2.2 ТЗ): за кодом із QR показуємо деталі
/// квитка (фільм/зал/час/місце/власник) БЕЗ зміни статусу — контролер має побачити, кого
/// саме впускає, перш ніж натиснути "Пропустити" (окрема команда ValidateTicket).
/// </summary>
public record GetTicketByCodeQuery(string TicketCode) : IRequest<TicketPreviewDto>;
