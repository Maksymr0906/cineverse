using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Tickets.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Tickets.Queries.GetTicketByCode;

public class GetTicketByCodeQueryHandler : IRequestHandler<GetTicketByCodeQuery, TicketPreviewDto>
{
    private readonly IApplicationDbContext _context;

    public GetTicketByCodeQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TicketPreviewDto> Handle(GetTicketByCodeQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Seat)
            .Include(t => t.Session).ThenInclude(s => s.Movie)
            .Include(t => t.Session).ThenInclude(s => s.Hall)
            .Include(t => t.Booking).ThenInclude(b => b.User)
            .FirstOrDefaultAsync(t => t.TicketCode == request.TicketCode, cancellationToken);

        if (ticket is null)
        {
            // Навмисно узагальнене повідомлення: контролер не повинен здогадуватись,
            // чи це "неправильний формат коду", чи "код підроблений" — обидва трактуються однаково.
            throw new NotFoundException("Квиток із таким кодом не знайдено. Перевірте QR-код.");
        }

        return new TicketPreviewDto(
            ticket.Id,
            ticket.TicketCode,
            ticket.Status.ToString(),
            ticket.Session.Movie.Title,
            ticket.Session.Hall.Name,
            ticket.Session.StartTime,
            ticket.Seat.RowNumber,
            ticket.Seat.SeatNumber,
            $"{ticket.Booking.User.FirstName} {ticket.Booking.User.LastName}",
            ticket.ValidatedAt
        );
    }
}
