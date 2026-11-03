namespace CineVerse.Application.Common.Interfaces;

/// <summary>Надсилання транзакційних листів (підтвердження email, квитки з QR-кодами).</summary>
public interface IEmailService
{
    Task SendEmailConfirmationAsync(string toEmail, string confirmationToken, CancellationToken cancellationToken = default);
    Task SendTicketsAsync(string toEmail, IEnumerable<Guid> ticketIds, CancellationToken cancellationToken = default);
    Task SendPasswordResetAsync(string toEmail, string resetToken, CancellationToken cancellationToken = default);
}
