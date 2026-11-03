using MediatR;

namespace CineVerse.Application.Features.Cinemas.Commands.UpdateHall;

/// <summary>
/// Редагування залу (кабінет адміністратора, п.2.5 ТЗ). Свідомо дозволяє міняти лише
/// Name і Type — НЕ Rows/SeatsPerRow/SpecialSeats. Зал може вже мати сеанси й бронювання,
/// прив'язані до конкретних Seat.Id (BookingSeat.SeatId), тому перегенерація розсадки для
/// існуючого залу зламала б історію бронювань точно так само, як зміна MovieId зламала б
/// уже продані квитки на сеанс — той самий принцип, що і в UpdateSession.
/// Якщо колись знадобиться міняти саму розсадку — це має бути окрема операція з явною
/// перевіркою відсутності активних (PENDING/CONFIRMED) бронювань у залі, а не звичайний Update.
/// </summary>
public record UpdateHallCommand(Guid HallId, string Name, string Type) : IRequest;
