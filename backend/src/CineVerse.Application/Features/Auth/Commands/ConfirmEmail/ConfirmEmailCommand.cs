using MediatR;

namespace CineVerse.Application.Features.Auth.Commands.ConfirmEmail;

/// <summary>Підтвердження email за токеном, надісланим на пошту при реєстрації.</summary>
public record ConfirmEmailCommand(string Token) : IRequest;
