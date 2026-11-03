using CineVerse.Application.Features.Auth.Common;
using MediatR;

namespace CineVerse.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<AuthResultDto>;
