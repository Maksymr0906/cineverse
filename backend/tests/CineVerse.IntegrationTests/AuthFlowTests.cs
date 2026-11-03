using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CineVerse.Infrastructure.Persistence;
using CineVerse.IntegrationTests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CineVerse.IntegrationTests;

/// <summary>
/// Наскрізний Auth-флоу (Етап 3) через реальний HTTP-стек: реєстрація → підтвердження email →
/// логін → звернення до захищеного /auth/me з отриманим access-токеном.
/// EmailConfirmationToken дістаємо напряму з БД (а не з листа) — EmailService у Development-режимі
/// без SendGrid:ApiKey лише логує лист, реального листа не існує (див. Етап 3, README).
/// </summary>
public class AuthFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullFlow_Register_ConfirmEmail_Login_AccessProtectedEndpoint()
    {
        var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@integration.test";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Інтеграційний",
            lastName = "Тест",
            email,
            phone = $"+380{Random.Shared.Next(100000000, 999999999)}",
            password = "IntegrationTest1"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmationToken = await GetEmailConfirmationTokenAsync(email);
        confirmationToken.Should().NotBeNullOrEmpty();

        var confirmResponse = await client.GetAsync($"/api/auth/confirm-email?token={confirmationToken}");
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "IntegrationTest1" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginBody.GetProperty("accessToken").GetString();
        accessToken.Should().NotBeNullOrEmpty();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@integration.test";
        var payload = new
        {
            firstName = "Дублікат", lastName = "Тест", email,
            phone = $"+380{Random.Shared.Next(100000000, 999999999)}", password = "IntegrationTest1"
        };

        await client.PostAsJsonAsync("/api/auth/register", payload);
        var secondAttempt = await client.PostAsJsonAsync("/api/auth/register", payload with { phone = $"+380{Random.Shared.Next(100000000, 999999999)}" });

        secondAttempt.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        // Використовуємо seed-акаунт адміністратора (Етап 2) — не потребує окремої реєстрації.
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@cineverse.local", password = "WrongPassword" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<string?> GetEmailConfirmationTokenAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        return user?.EmailConfirmationToken;
    }
}
