namespace CineVerse.Application.Common.Interfaces;

/// <summary>Генерація QR-кодів для електронних квитків (QRCoder, п.2.2 ТЗ).</summary>
public interface IQrCodeGenerator
{
    /// <summary>Повертає QR-код як data URI (base64 PNG), готовий для прямого використання в &lt;img src="..."&gt;.</summary>
    string GenerateBase64Png(string content);
}
