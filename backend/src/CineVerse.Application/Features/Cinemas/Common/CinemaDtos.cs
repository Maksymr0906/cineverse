namespace CineVerse.Application.Features.Cinemas.Common;

public record HallDto(Guid Id, string Name, string Type, int Capacity);

public record CinemaDto(Guid Id, string Name, string Address, string City, string Phone, IReadOnlyList<HallDto> Halls);
