using MediatR;

namespace CineVerse.Application.Features.Sessions.Commands.DeleteSession;

public record DeleteSessionCommand(Guid SessionId) : IRequest;
