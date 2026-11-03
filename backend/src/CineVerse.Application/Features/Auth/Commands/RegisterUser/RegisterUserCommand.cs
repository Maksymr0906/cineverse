using MediatR;

namespace CineVerse.Application.Features.Auth.Commands.RegisterUser;

/// <summary>Реєстрація нового клієнта (п.2.2 ТЗ). Повертає Id створеного користувача.</summary>
public record RegisterUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Password
) : IRequest<Guid>;
