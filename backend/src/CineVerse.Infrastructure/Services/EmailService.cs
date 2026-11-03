using CineVerse.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CineVerse.Infrastructure.Services;

/// <summary>
/// Надсилання транзакційних листів через SendGrid (п.1.5.1 ТЗ).
/// Якщо SendGrid:ApiKey не налаштовано (типово для локальної розробки) — лист не відправляється,
/// а лише логується, щоб розробник міг узяти токен/посилання з логів без реального email-акаунту.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailConfirmationAsync(string toEmail, string confirmationToken, CancellationToken cancellationToken = default)
    {
        var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var confirmationLink = $"{frontendUrl}/confirm-email?token={confirmationToken}";

        var subject = "CineVerse — підтвердження реєстрації";
        var htmlContent = $"""
            <h2>Ласкаво просимо до CineVerse!</h2>
            <p>Щоб завершити реєстрацію, підтвердіть свою електронну адресу:</p>
            <p><a href="{confirmationLink}">Підтвердити email</a></p>
            <p>Якщо ви не реєструвались у CineVerse — просто проігноруйте цей лист.</p>
            """;

        await SendAsync(toEmail, subject, htmlContent, cancellationToken);
    }

    public async Task SendTicketsAsync(string toEmail, IEnumerable<Guid> ticketIds, CancellationToken cancellationToken = default)
    {
        // TODO (Етап 6 — Оплата та квитки): вкладення PDF з QR-кодами (QRCoder) генеруються
        // у відповідному хендлері обробки Webhook і передаються сюди як attachments.
        var subject = "CineVerse — ваші електронні квитки";
        var htmlContent = $"<p>Ваші квитки ({ticketIds.Count()} шт.) додано до особистого кабінету.</p>";

        await SendAsync(toEmail, subject, htmlContent, cancellationToken);
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetToken, CancellationToken cancellationToken = default)
    {
        var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var resetLink = $"{frontendUrl}/reset-password?token={resetToken}";

        var subject = "CineVerse — відновлення пароля";
        var htmlContent = $"""
            <h2>Відновлення пароля</h2>
            <p>Ви (або хтось від вашого імені) запросили скидання пароля для акаунту CineVerse.</p>
            <p><a href="{resetLink}">Встановити новий пароль</a></p>
            <p>Посилання дійсне 30 хвилин. Якщо ви не робили цей запит — просто проігноруйте лист,
            пароль залишиться незмінним.</p>
            """;

        await SendAsync(toEmail, subject, htmlContent, cancellationToken);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlContent, CancellationToken cancellationToken)
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        var fromEmail = _configuration["SendGrid:FromEmail"] ?? "noreply@cineverse.local";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogInformation(
                "[DEV MODE] SendGrid:ApiKey не налаштовано. Лист до {ToEmail} не надіслано, вміст:\nТема: {Subject}\n{Content}",
                toEmail, subject, htmlContent);
            return;
        }

        var client = new SendGridClient(apiKey);
        var msg = MailHelper.CreateSingleEmail(
            from: new EmailAddress(fromEmail, "CineVerse"),
            to: new EmailAddress(toEmail),
            subject: subject,
            plainTextContent: null,
            htmlContent: htmlContent);

        var response = await client.SendEmailAsync(msg, cancellationToken);
        if ((int)response.StatusCode >= 400)
        {
            _logger.LogWarning("SendGrid повернув помилку {StatusCode} при надсиланні листа до {ToEmail}", response.StatusCode, toEmail);
        }
    }
}
