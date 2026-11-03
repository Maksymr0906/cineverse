using MediatR;

namespace CineVerse.Application.Features.Cinemas.Commands.CreateCinema;

/// <summary>Додавання нового кінотеатру мережі (кабінет адміністратора, п.2.5 ТЗ).</summary>
public record CreateCinemaCommand(string Name, string Address, string City, string Phone) : IRequest<Guid>;
