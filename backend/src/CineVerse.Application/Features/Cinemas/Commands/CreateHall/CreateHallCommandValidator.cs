using CineVerse.Domain.Enums;
using FluentValidation;

namespace CineVerse.Application.Features.Cinemas.Commands.CreateHall;

public class CreateHallCommandValidator : AbstractValidator<CreateHallCommand>
{
    public CreateHallCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Type).Must(t => Enum.TryParse<HallType>(t, true, out _))
            .WithMessage($"Тип залу має бути одним із: {string.Join(", ", Enum.GetNames<HallType>())}");
        RuleFor(x => x.Rows).InclusiveBetween(1, 50);
        RuleFor(x => x.SeatsPerRow).InclusiveBetween(1, 50);

        RuleForEach(x => x.SpecialSeats).ChildRules(seat =>
        {
            seat.RuleFor(s => s.SeatType).Must(t => Enum.TryParse<SeatType>(t, true, out _))
                .WithMessage($"Тип місця має бути одним із: {string.Join(", ", Enum.GetNames<SeatType>())}");
        });

        // Виправлення після аудиту: раніше дублікати координат спецмісць проходили валідацію
        // і падали необробленим ArgumentException у хендлері (.ToDictionary на дублікаті ключа),
        // а координати поза межами залу мовчки ігнорувались без жодного повідомлення адміністратору.
        RuleFor(x => x.SpecialSeats)
            .Must(seats => seats.Select(s => (s.RowNumber, s.SeatNumber)).Distinct().Count() == seats.Count)
            .WithMessage("У списку особливих місць є повторювані координати (ряд+місце).");

        RuleFor(x => x)
            .Must(cmd => cmd.SpecialSeats.All(s => s.RowNumber >= 1 && s.RowNumber <= cmd.Rows))
            .WithMessage(cmd => $"Номер ряду в особливих місцях має бути від 1 до {cmd.Rows}.")
            .Must(cmd => cmd.SpecialSeats.All(s => s.SeatNumber >= 1 && s.SeatNumber <= cmd.SeatsPerRow))
            .WithMessage(cmd => $"Номер місця в ряду в особливих місцях має бути від 1 до {cmd.SeatsPerRow}.");
    }
}
