using MediatR;

namespace CineVerse.Application.Features.Auth.Commands.Logout;

/// <summary>Інвалідує збережений refresh-токен користувача (вихід з усіх сесій на цьому пристрої).</summary>
public record LogoutCommand(Guid UserId) : IRequest;
