using MediatR;

namespace CineVerse.Application.Features.Sessions.Commands.CreateSession;

/// <summary>
/// Додавання сеансу до розкладу (кабінет адміністратора/менеджера, п.2.5 ТЗ).
/// EndTime обчислюється сервером = StartTime + тривалість фільму + технічна перерва
/// (SessionScheduling.CleaningBufferMinutes), і саме на цей інтервал перевіряється
/// відсутність накладок з іншими сеансами в тому самому залі.
/// </summary>
public record CreateSessionCommand(
    Guid MovieId,
    Guid HallId,
    DateTimeOffset StartTime,
    string Format,
    decimal BasePrice
) : IRequest<Guid>;
