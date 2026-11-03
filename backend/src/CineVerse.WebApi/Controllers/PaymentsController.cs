using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Payments.Commands.ConfirmCashPayment;
using CineVerse.Application.Features.Payments.Commands.InitiatePayment;
using CineVerse.Application.Features.Payments.Commands.ProcessPaymentWebhook;
using CineVerse.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVerse.WebApi.Controllers;

public record InitiatePaymentRequest(Guid BookingId);
public record MockCompleteRequest(bool Success);

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IPaymentGatewayService _paymentGateway;

    public PaymentsController(IMediator mediator, ICurrentUserService currentUser, IPaymentGatewayService paymentGateway)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _paymentGateway = paymentGateway;
    }

    /// <summary>Ініціює оплату бронювання, повертає посилання на платіжну сторінку.</summary>
    [Authorize]
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate([FromBody] InitiatePaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new InitiatePaymentCommand(request.BookingId, _currentUser.UserId!.Value), cancellationToken);
        return Ok(result);
    }

    /// <summary>Готівкова оплата на касі (роль CASHIER/ADMIN/MANAGER).</summary>
    [Authorize(Policy = "StaffOnly")]
    [HttpPost("{bookingId:guid}/cash")]
    public async Task<IActionResult> ConfirmCash(Guid bookingId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ConfirmCashPaymentCommand(bookingId, _currentUser.UserId!.Value), cancellationToken);
        return Ok(new { message = "Оплату готівкою підтверджено, квитки видано." });
    }

    /// <summary>
    /// Справжній Webhook-ендпоінт від платіжного шлюзу. Підпис передається в заголовку
    /// (для LiqPay/Stripe — стандартна практика; назва заголовка залежить від конкретного шлюзу
    /// і буде уточнена при підключенні реального SDK).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromHeader(Name = "X-Signature")] string signature, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawPayload = await reader.ReadToEndAsync(cancellationToken);

        var payload = System.Text.Json.JsonSerializer.Deserialize<Application.Features.Payments.Common.WebhookPayload>(
            rawPayload, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new Application.Common.Exceptions.ValidationException();

        await _mediator.Send(new ProcessPaymentWebhookCommand(
            rawPayload, signature, payload.TransactionId, payload.Status.Equals("success", StringComparison.OrdinalIgnoreCase)),
            cancellationToken);

        return Ok();
    }

    /// <summary>
    /// DEV-ONLY: імітує повернення з банку для локального тестування без реального LiqPay/Stripe.
    /// Формує коректно підписаний payload і прогонить його через ТОЙ САМИЙ обробник Webhook,
    /// що й реальний шлюз, — тому цей ендпоінт перевіряє саме бізнес-логіку, а не є "чорним ходом".
    /// Доступність визначається не середовищем (ASPNETCORE_ENVIRONMENT), а тим, чи зараз узагалі
    /// зареєстрований mock-шлюз — див. перевірку нижче. У проєкті поки немає реального LiqPay/Stripe,
    /// тож прив'язка до Development раніше блокувала єдиний робочий спосіб завершити оплату навіть
    /// у "Production"-подібному docker-compose, який студент використовує для власного демо.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("mock/{transactionId}/complete")]
    public async Task<IActionResult> MockComplete(string transactionId, [FromBody] MockCompleteRequest request, CancellationToken cancellationToken)
    {
        if (_paymentGateway is not MockPaymentGatewayService mockGateway)
        {
            // Захист на випадок, якщо колись підключать реальний шлюз (LiqPay/Stripe) і забудуть
            // прибрати цей маршрут — без mock-реалізації немає кого "симулювати".
            return NotFound(new { message = "Mock-платіжний шлюз не зареєстровано в цьому середовищі." });
        }

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            TransactionId = transactionId,
            Status = request.Success ? "success" : "failed"
        });
        var signature = mockGateway.ComputeHmacForCurrentSecret(payload);

        await _mediator.Send(new ProcessPaymentWebhookCommand(payload, signature, transactionId, request.Success), cancellationToken);

        return Ok(new { message = request.Success ? "Оплату імітовано успішно." : "Оплату імітовано як невдалу." });
    }
}
