using MediatR;

namespace CineVerse.Application.Features.Sessions.Commands.UpdateSession;

/// <summary>Перенесення сеансу на інший час/зал/формат/ціну з повторною перевіркою конфлікту розкладу.</summary>
public record UpdateSessionCommand(
    Guid SessionId,
    Guid HallId,
    DateTimeOffset StartTime,
    string Format,
    decimal BasePrice
) : IRequest;
