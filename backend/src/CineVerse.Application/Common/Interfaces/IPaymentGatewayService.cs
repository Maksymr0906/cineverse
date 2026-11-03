namespace CineVerse.Application.Common.Interfaces;

public record PaymentSessionResult(string RedirectUrl, string GatewayTransactionId);

/// <summary>
/// Абстракція над платіжним шлюзом (LiqPay/Stripe, п.1.5.1 ТЗ). Поточна реалізація в
/// Infrastructure — MockPaymentGatewayService: імітує шлюз локально, без реального
/// зовнішнього виклику (у цьому середовищі розробки немає мережевого доступу до LiqPay/Stripe).
/// Контракт спроєктовано так, щоб заміна на реальний шлюз не торкалась Application-шару.
/// </summary>
public interface IPaymentGatewayService
{
    Task<PaymentSessionResult> CreatePaymentSessionAsync(Guid paymentId, decimal amount, string description, CancellationToken cancellationToken = default);

    /// <summary>Перевірка підпису вхідного Webhook (HMAC-SHA256 над сирим тілом запиту із секретом шлюзу).</summary>
    bool VerifyWebhookSignature(string payload, string signature);
}
