using MediatR;

namespace CineVerse.Application.Features.Payments.Commands.ProcessPaymentWebhook;

/// <summary>
/// Обробка вхідного Webhook від платіжного шлюзу. RawPayload+Signature перевіряються ДО
/// десеріалізації бізнес-даних (IPaymentGatewayService.VerifyWebhookSignature) — це стандартна
/// практика захисту Webhook-ендпоінтів від підробних запитів.
/// </summary>
public record ProcessPaymentWebhookCommand(string RawPayload, string Signature, string TransactionId, bool IsSuccess) : IRequest;
