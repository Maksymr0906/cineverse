using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Cinemas.Commands.CreateHall;

public class CreateHallCommandHandler : IRequestHandler<CreateHallCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateHallCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateHallCommand request, CancellationToken cancellationToken)
    {
        var cinemaExists = await _context.Cinemas.AnyAsync(c => c.Id == request.CinemaId, cancellationToken);
        if (!cinemaExists)
        {
            throw new NotFoundException(nameof(Cinema), request.CinemaId);
        }

        var hall = new Hall
        {
            CinemaId = request.CinemaId,
            Name = request.Name,
            Type = Enum.Parse<HallType>(request.Type, true),
            Capacity = request.Rows * request.SeatsPerRow
        };

        var specialByCoords = request.SpecialSeats
            .ToDictionary(s => (s.RowNumber, s.SeatNumber), s => Enum.Parse<SeatType>(s.SeatType, true));

        for (var row = 1; row <= request.Rows; row++)
        {
            for (var seatNum = 1; seatNum <= request.SeatsPerRow; seatNum++)
            {
                var seatType = specialByCoords.GetValueOrDefault((row, seatNum), SeatType.STANDARD);
                hall.Seats.Add(new Seat { HallId = hall.Id, RowNumber = row, SeatNumber = seatNum, Type = seatType });
            }
        }

        _context.Halls.Add(hall);
        await _context.SaveChangesAsync(cancellationToken);

        return hall.Id;
    }
}
