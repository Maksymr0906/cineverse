using Microsoft.AspNetCore.Mvc;

namespace CineVerse.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>Простий healthcheck-ендпоінт для перевірки, що API запущено (для Docker/CI).</summary>
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", service = "CineVerse.WebApi", timestamp = DateTimeOffset.UtcNow });
}
