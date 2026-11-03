using System.Security.Cryptography;
using System.Text;
using CineVerse.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CineVerse.Infrastructure.Services;

/// <summary>
/// Тимчасова (mock) реалізація платіжного шлюзу — це середовище розробки не має мережевого
/// доступу до реального LiqPay/Stripe API. Контракт (IPaymentGatewayService) спроєктовано так,
/// щоб заміна на реальний SDK не торкалась жодного коду в Application-шарі: достатньо
/// зареєструвати іншу реалізацію в Infrastructure/DependencyInjection.cs.
///
/// "Redirect URL" тут веде на власну mock-сторінку оплати фронтенду (/mock-payment/{transactionId}),
/// яка дає користувачу кнопки "Оплата успішна" / "Скасувати" — це і імітує повернення з банку.
/// Підпис Webhook — справжній HMAC-SHA256 із секретом конфігурації, щоб механізм перевірки
/// підпису залишався ідентичним до того, що буде з реальним шлюзом.
///
/// ВАЖЛИВО (виправлення після аудиту): PaymentGateway:WebhookSecret раніше мав "тихий" fallback
/// на публічно відомий рядок "dev_only_mock_webhook_secret" НАВІТЬ у продакшн-конфігурації.
/// Оскільки цей секрет підписує запити до СПРАВЖНЬОГО ендпоінта /api/payments/webhook (не лише
/// dev-only /payments/mock/...), відомий усім секрет дозволив би будь-кому підробити Webhook
/// "успішної оплати" й отримати квитки безкоштовно. Тепер fallback діє лише в Development;
/// у будь-якому іншому середовищі відсутній секрет — це фатальна помилка конфігурації.
/// </summary>
public class MockPaymentGatewayService : IPaymentGatewayService
{
    private readonly IConfiguration _configuration;
    private readonly string _webhookSecret;

    public MockPaymentGatewayService(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;

        var configuredSecret = configuration["PaymentGateway:WebhookSecret"];
        if (!string.IsNullOrWhiteSpace(configuredSecret))
        {
            _webhookSecret = configuredSecret;
        }
        else if (environment.IsDevelopment())
        {
            _webhookSecret = "dev_only_mock_webhook_secret";
        }
        else
        {
            throw new InvalidOperationException(
                "PaymentGateway:WebhookSecret не налаштовано. Це критично: цей секрет захищає справжній " +
                "ендпоінт /api/payments/webhook від підроблених запитів. Задайте його через змінну середовища " +
                "PaymentGateway__WebhookSecret — тут навмисно немає fallback-значення поза Development.");
        }
    }

    public Task<PaymentSessionResult> CreatePaymentSessionAsync(Guid paymentId, decimal amount, string description, CancellationToken cancellationToken = default)
    {
        var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var transactionId = $"MOCK-{Guid.NewGuid():N}";
        var redirectUrl = $"{frontendUrl}/mock-payment/{transactionId}?paymentId={paymentId}&amount={amount}";

        return Task.FromResult(new PaymentSessionResult(redirectUrl, transactionId));
    }

    public bool VerifyWebhookSignature(string payload, string signature)
    {
        var expected = ComputeHmac(payload, _webhookSecret);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(expected),
            TryDecodeHex(signature));
    }

    /// <summary>Допоміжний метод для MockPaymentPage/PaymentsController.MockComplete, щоб згенерувати
    /// коректний підпис так само, як мав би це робити реальний банк на своєму боці.</summary>
    public string ComputeHmacForCurrentSecret(string payload) => ComputeHmac(payload, _webhookSecret);

    private static string ComputeHmac(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }

    private static byte[] TryDecodeHex(string value)
    {
        try { return Convert.FromHexString(value); }
        catch (FormatException) { return Array.Empty<byte>(); }
    }
}
