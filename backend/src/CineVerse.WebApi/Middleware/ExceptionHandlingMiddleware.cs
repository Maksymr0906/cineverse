using System.Net;
using System.Text.Json;
using CineVerse.Application.Common.Exceptions;
using ValidationException = CineVerse.Application.Common.Exceptions.ValidationException;

namespace CineVerse.WebApi.Middleware;

/// <summary>
/// Глобальний перехоплювач винятків: перетворює бізнес-винятки Application-шару
/// на консистентні JSON-відповіді з правильними HTTP-кодами, замість "сирих" 500-помилок.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, errors) = exception switch
        {
            ValidationException vex => (HttpStatusCode.BadRequest, "Помилка валідації", (object?)vex.Errors),
            NotFoundException => (HttpStatusCode.NotFound, exception.Message, null),
            ConflictException => (HttpStatusCode.Conflict, exception.Message, null),
            AuthenticationException => (HttpStatusCode.Unauthorized, exception.Message, null),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Недостатньо прав для виконання цієї дії.", null),
            _ => (HttpStatusCode.InternalServerError, "Виникла непередбачена помилка сервера.", null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Необроблений виняток під час обробки {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(new
        {
            status = (int)statusCode,
            title,
            errors
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(payload);
    }
}
