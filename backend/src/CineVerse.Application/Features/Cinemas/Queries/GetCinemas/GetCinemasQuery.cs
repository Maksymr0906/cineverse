using CineVerse.Application.Features.Cinemas.Common;
using MediatR;

namespace CineVerse.Application.Features.Cinemas.Queries.GetCinemas;

/// <summary>Список кінотеатрів мережі із залами; опційний фільтр за містом (п.2.1 ТЗ: вибір міста/кінотеатру).</summary>
public record GetCinemasQuery(string? City = null) : IRequest<IReadOnlyList<CinemaDto>>;
