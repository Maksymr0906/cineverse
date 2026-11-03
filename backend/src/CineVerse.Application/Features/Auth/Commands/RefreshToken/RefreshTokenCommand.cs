using CineVerse.Application.Features.Auth.Common;
using MediatR;

namespace CineVerse.Application.Features.Auth.Commands.RefreshToken;

/// <summary>Оновлення пари токенів за дійсним refresh-токеном (ротація: старий інвалідується, видається новий).</summary>
public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResultDto>;
