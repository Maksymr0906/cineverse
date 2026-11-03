using CineVerse.Application.Features.Tickets.Common;
using MediatR;

namespace CineVerse.Application.Features.Tickets.Commands.ValidateTicket;

/// <summary>Крок 2 сканування: контролер підтверджує прохід. Незворотно переводить квиток у USED.</summary>
public record ValidateTicketCommand(string TicketCode, Guid CheckerUserId) : IRequest<TicketValidationResultDto>;
