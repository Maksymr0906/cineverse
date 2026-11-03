namespace CineVerse.Application.Features.Payments.Common;

public record PaymentInitiationResultDto(Guid PaymentId, string RedirectUrl);

public record WebhookPayload(string TransactionId, string Status); // "success" | "failed"
