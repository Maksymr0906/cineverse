using MediatR;

namespace CineVerse.Application.Features.Cinemas.Commands.CreateHall;

/// <summary>
/// Створення залу разом із автогенерацією схеми місць (рівномірна сітка "ряди x місця"
/// + опційні "особливі" місця: VIP/Love Seat/Disabled за конкретними координатами).
/// Відповідає п.2.5 ТЗ: "Управління залами: додавання/редагування схеми розсадки".
/// </summary>
public record CreateHallCommand(
    Guid CinemaId,
    string Name,
    string Type,
    int Rows,
    int SeatsPerRow,
    IReadOnlyList<SpecialSeatInput> SpecialSeats
) : IRequest<Guid>;

public record SpecialSeatInput(int RowNumber, int SeatNumber, string SeatType);
