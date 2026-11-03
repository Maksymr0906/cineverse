using CineVerse.Application.Common.Interfaces;
using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CineVerse.Application.Features.Tickets.Services;

/// <summary>
/// Спільна логіка видачі електронних квитків після успішної оплати — незалежно від того,
/// прийшла вона через онлайн-шлюз (Webhook) чи готівкою в касі (ConfirmCashPayment).
/// Кожному BookingSeat відповідає один Ticket з унікальним кодом та QR-кодом (п.2.2 ТЗ).
///
/// ВИПРАВЛЕННЯ ПІСЛЯ АУДИТУ: раніше цей сервіс сам викликав SaveChangesAsync окремо від
/// зміни статусів Booking/Payment у виклика́ючому хендлері — тобто "оплата підтверджена"
/// і "квитки створено" були ДВОМА окремими транзакціями. Якщо друга (створення квитків)
/// падала — наприклад, через колізію унікального TicketCode чи будь-яку іншу помилку БД —
/// гроші вже вважалися прийнятими, а квитків не було, і повторний Webhook від шлюзу
/// ігнорувався ідемпотентність-guard'ом (Payment.Status уже не PENDING) НАЗАВЖДИ.
///
/// Тепер PrepareTickets лише додає сутності в контекст, не викликаючи SaveChanges — виклик
/// зберігає їх РАЗОМ з Booking.Status/Payment.Status у ЄДИНОМУ SaveChangesAsync виклика́ючого
/// хендлера. Якщо щось піде не так — відкотиться ВСЕ (включно з Payment.Status), і той-таки
/// ідемпотентність-guard коректно дозволить повторну спробу при retry від платіжного шлюзу.
/// Надсилання email навмисно винесено ЗА межі цієї атомарності (SendTicketEmailSafeAsync)
/// і ніколи не кидає виняток нагору — збій пошти не повинен позначатися на вже зафіксованій
/// оплаті й виданих квитках.
/// </summary>
public class TicketIssuanceService
{
    private readonly IApplicationDbContext _context;
    private readonly IQrCodeGenerator _qrCodeGenerator;
    private readonly IEmailService _emailService;
    private readonly ILogger<TicketIssuanceService> _logger;

    public TicketIssuanceService(
        IApplicationDbContext context,
        IQrCodeGenerator qrCodeGenerator,
        IEmailService emailService,
        ILogger<TicketIssuanceService> logger)
    {
        _context = context;
        _qrCodeGenerator = qrCodeGenerator;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>Готує Ticket-сутності для кожного місця бронювання й додає їх у контекст
    /// БЕЗ виклику SaveChangesAsync — виклик має бути частиною ширшої атомарної транзакції.</summary>
    public List<Ticket> PrepareTickets(Booking booking)
    {
        var tickets = new List<Ticket>();

        foreach (var bookingSeat in booking.BookingSeats)
        {
            // Повний GUID (32 hex-символи, ~122 біти ентропії) — раніше код обрізався до 14 символів
            // (лише ~44 біти), що на масштабі мільйонів квитків давало реальний ризик колізії
            // через парадокс днів народження. VARCHAR(50) у схемі БД із запасом вміщує повний код.
            var ticketCode = $"CV-{Guid.NewGuid():N}".ToUpperInvariant();

            var ticket = new Ticket
            {
                BookingId = booking.Id,
                SessionId = booking.SessionId,
                SeatId = bookingSeat.SeatId,
                TicketCode = ticketCode,
                QrCode = _qrCodeGenerator.GenerateBase64Png(ticketCode),
                Status = TicketStatus.ACTIVE
            };

            tickets.Add(ticket);
            _context.Tickets.Add(ticket);
        }

        return tickets;
    }

    /// <summary>Надсилає лист із квитками ПІСЛЯ успішного збереження в БД. Ніколи не кидає
    /// виняток нагору — недоступність SendGrid не повинна перетворюватись на 500 для клієнта,
    /// коли гроші й квитки вже коректно зафіксовані.</summary>
    public async Task SendTicketEmailSafeAsync(string userEmail, IEnumerable<Guid> ticketIds, CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendTicketsAsync(userEmail, ticketIds, cancellationToken);
        }
        catch (Exception ex)
        {
            // TODO: у продакшн-варіанті сюди варто додати чергу повторних спроб (напр. Hangfire/outbox),
            // щоб користувач гарантовано отримав лист навіть після тимчасового збою SendGrid.
            // Квитки при цьому лишаються доступні в особистому кабінеті незалежно від статусу листа.
            _logger.LogError(ex, "Не вдалося надіслати лист із квитками на {Email}. Квитки збережено в БД, лист НЕ надіслано.", userEmail);
        }
    }
}
