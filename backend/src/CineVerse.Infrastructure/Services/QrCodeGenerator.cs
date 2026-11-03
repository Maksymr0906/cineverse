using CineVerse.Application.Common.Interfaces;
using QRCoder;

namespace CineVerse.Infrastructure.Services;

/// <summary>Генерація QR-кодів через QRCoder (п.2.2 ТЗ). Кодує сам TicketCode — контролер (Етап 7)
/// сканує QR, отримує код і звіряє його з БД через /api/tickets/validate.</summary>
public class QrCodeGenerator : IQrCodeGenerator
{
    public string GenerateBase64Png(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var pngQrCode = new PngByteQRCode(qrCodeData);
        var bytes = pngQrCode.GetGraphic(20);

        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}
