using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Tickets.Common;
using CineVerse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CineVerse.Application.Features.Tickets.Commands.ValidateTicket;

/// <summary>
/// Бізнес-правила валідації квитка на вході в зал (п.2.2 ТЗ: "кабінет контролера — сканування
/// QR, перевірка дійсності квитка, захист від повторного використання"):
///   1. Квиток має існувати і мати статус ACTIVE (не USED, не CANCELLED).
///   2. Вхід відкривається за EarlyEntryMinutes хвилин до старту сеансу і закривається
///      в момент його завершення.
///
/// Захист від подвійного сканування: сам факт "прочитати статус, потім записати USED"
/// у два окремі кроки НЕ є атомарним і теоретично вразливий до такої ж Race Condition,
/// як бронювання місць (Етап 5) — якщо квиток одночасно показали на двох пристроях
/// двом контролерам. Тому фінальний перехід у USED виконується єдиним умовним
/// SQL UPDATE (ExecuteUpdateAsync: "UPDATE ... WHERE Id = @id AND Status = 'ACTIVE'")
/// замість "прочитати сутність → змінити властивість → SaveChanges". Це гарантує, що
/// з двох одночасних сканувань лише одне реально змінить рядок (affected rows = 1),
/// а друге побачить affected rows = 0 і отримає 409 — так само, як Redis-лок відсікає
/// повторне бронювання одного місця.
/// </summary>
public class ValidateTicketCommandHandler : IRequestHandler<ValidateTicketCommand, TicketValidationResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;
    private readonly IConfiguration _configuration;

    public ValidateTicketCommandHandler(IApplicationDbContext context, IDateTime dateTime, IConfiguration configuration)
    {
        _context = context;
        _dateTime = dateTime;
        _configuration = configuration;
    }

    public async Task<TicketValidationResultDto> Handle(ValidateTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Seat)
            .Include(t => t.Session)
            .FirstOrDefaultAsync(t => t.TicketCode == request.TicketCode, cancellationToken)
            ?? throw new NotFoundException("Квиток із таким кодом не знайдено. Перевірте QR-код.");

        if (ticket.Status == TicketStatus.USED)
        {
            throw new ConflictException($"Квиток уже використано {ticket.ValidatedAt:dd.MM.yyyy HH:mm}. Повторний прохід за цим квитком заборонено.");
        }

        if (ticket.Status == TicketStatus.CANCELLED)
        {
            throw new ConflictException("Цей квиток скасовано і недійсний для проходу.");
        }

        var earlyEntryMinutes = int.TryParse(_configuration["TicketValidation:EarlyEntryMinutes"], out var m) ? m : 30;
        var now = _dateTime.UtcNow;
        var entryOpensAt = ticket.Session.StartTime.AddMinutes(-earlyEntryMinutes);

        if (now < entryOpensAt)
        {
            throw new ConflictException(
                $"Занадто рано: вхід у зал відкриється о {entryOpensAt:HH:mm}, за {earlyEntryMinutes} хв до початку сеансу.");
        }

        if (now > ticket.Session.EndTime)
        {
            throw new ConflictException("Сеанс, на який куплено цей квиток, уже завершився.");
        }

        // Атомарний умовний UPDATE — див. коментар до класу.
        var affectedRows = await _context.Tickets
            .Where(t => t.Id == ticket.Id && t.Status == TicketStatus.ACTIVE)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Status, TicketStatus.USED)
                .SetProperty(t => t.ValidatedAt, now)
                .SetProperty(t => t.ValidatedByUserId, request.CheckerUserId),
                cancellationToken);

        if (affectedRows == 0)
        {
            throw new ConflictException("Цей квиток щойно було використано іншим сканером. Прохід заборонено.");
        }

        return new TicketValidationResultDto(
            ticket.Id, ticket.TicketCode, now, $"Ряд {ticket.Seat.RowNumber}, місце {ticket.Seat.SeatNumber}");
    }
}
