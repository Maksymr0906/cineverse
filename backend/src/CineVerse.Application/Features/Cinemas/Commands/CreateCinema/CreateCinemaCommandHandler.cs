using CineVerse.Application.Common.Interfaces;
using CineVerse.Domain.Entities;
using MediatR;

namespace CineVerse.Application.Features.Cinemas.Commands.CreateCinema;

public class CreateCinemaCommandHandler : IRequestHandler<CreateCinemaCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateCinemaCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateCinemaCommand request, CancellationToken cancellationToken)
    {
        var cinema = new Cinema
        {
            Name = request.Name,
            Address = request.Address,
            City = request.City,
            Phone = request.Phone
        };

        _context.Cinemas.Add(cinema);
        await _context.SaveChangesAsync(cancellationToken);
        return cinema.Id;
    }
}
