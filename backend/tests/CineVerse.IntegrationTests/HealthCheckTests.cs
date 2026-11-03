using System.Net;
using CineVerse.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace CineVerse.IntegrationTests;

/// <summary>Найпростіший "дим-тест": якщо це падає — щось не так із самою збіркою DI-графа
/// (відсутня реєстрація сервісу, неправильний конектор до БД тощо), а не з бізнес-логікою.</summary>
public class HealthCheckTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public HealthCheckTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
