using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Features.Auth.Commands.ConfirmEmail;
using CineVerse.Application.Features.Auth.Commands.ForgotPassword;
using CineVerse.Application.Features.Auth.Commands.Login;
using CineVerse.Application.Features.Auth.Commands.Logout;
using CineVerse.Application.Features.Auth.Commands.RefreshToken;
using CineVerse.Application.Features.Auth.Commands.RegisterUser;
using CineVerse.Application.Features.Auth.Commands.ResetPassword;
using CineVerse.Application.Features.Auth.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVerse.WebApi.Controllers;

public record RegisterRequest(string FirstName, string LastName, string Email, string Phone, string Password);
public record LoginRequest(string Email, string Password);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "cineverse_refresh_token";
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Реєстрація нового клієнта. Лист з посиланням підтвердження надсилається на email.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var userId = await _mediator.Send(new RegisterUserCommand(
            request.FirstName, request.LastName, request.Email, request.Phone, request.Password), cancellationToken);

        return Ok(new { userId, message = "Реєстрація успішна. Перевірте пошту для підтвердження email." });
    }

    /// <summary>Підтвердження email за токеном з листа.</summary>
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string token, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ConfirmEmailCommand(token), cancellationToken);
        return Ok(new { message = "Email успішно підтверджено. Тепер ви можете увійти." });
    }

    /// <summary>Автентифікація за email/паролем. Access-токен — у тілі відповіді, refresh — у HttpOnly cookie.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        SetRefreshTokenCookie(result);
        return Ok(ToPublicResponse(result));
    }

    /// <summary>Запит на відновлення пароля. Завжди відповідає однаково, незалежно від існування email
    /// (захист від User Enumeration) — лист із посиланням надійде лише якщо акаунт справді існує.</summary>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ForgotPasswordCommand(request.Email), cancellationToken);
        return Ok(new { message = "Якщо такий email зареєстровано, на нього надіслано посилання для відновлення пароля." });
    }

    /// <summary>Встановлення нового пароля за токеном з листа (дійсний 30 хвилин).</summary>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ResetPasswordCommand(request.Token, request.NewPassword), cancellationToken);
        return Ok(new { message = "Пароль успішно змінено. Увійдіть з новим паролем." });
    }

    /// <summary>Оновлення пари токенів за refresh-токеном із cookie (ротація).</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new AuthenticationException("Refresh-токен відсутній. Увійдіть повторно.");
        }

        var result = await _mediator.Send(new RefreshTokenCommand(refreshToken), cancellationToken);
        SetRefreshTokenCookie(result);
        return Ok(ToPublicResponse(result));
    }

    /// <summary>Вихід: інвалідує refresh-токен на сервері та видаляє cookie.</summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(sub, out var userId))
        {
            await _mediator.Send(new LogoutCommand(userId), cancellationToken);
        }

        Response.Cookies.Delete(RefreshTokenCookieName);
        return Ok(new { message = "Ви вийшли з системи." });
    }

    /// <summary>Приклад захищеного ендпоінта — повертає claims поточного користувача. Корисно для перевірки токена.</summary>
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        return Ok(claims);
    }

    private void SetRefreshTokenCookie(AuthResultDto result)
    {
        Response.Cookies.Append(RefreshTokenCookieName, result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = result.RefreshTokenExpiresAt
        });
    }

    private static object ToPublicResponse(AuthResultDto result) => new
    {
        accessToken = result.AccessToken,
        user = new
        {
            id = result.UserId,
            firstName = result.FirstName,
            lastName = result.LastName,
            email = result.Email,
            role = result.Role
        }
    };
}
