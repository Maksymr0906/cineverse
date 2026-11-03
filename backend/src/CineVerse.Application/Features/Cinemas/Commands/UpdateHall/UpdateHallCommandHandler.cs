using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Cinemas.Commands.UpdateHall;

public class UpdateHallCommandHandler : IRequestHandler<UpdateHallCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateHallCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateHallCommand request, CancellationToken cancellationToken)
    {
        var hall = await _context.Halls.FirstOrDefaultAsync(h => h.Id == request.HallId, cancellationToken)
            ?? throw new NotFoundException(nameof(Hall), request.HallId);

        hall.Name = request.Name;
        hall.Type = Enum.Parse<HallType>(request.Type, true);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
