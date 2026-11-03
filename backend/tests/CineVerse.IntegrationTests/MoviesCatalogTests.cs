using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CineVerse.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace CineVerse.IntegrationTests;

/// <summary>Перевіряє, що 002_seed.sql справді накотився і публічний каталог його бачить —
/// це заразом і тест на GetMoviesQueryHandler, і на коректність seed-даних.</summary>
public class MoviesCatalogTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MoviesCatalogTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMovies_ReturnsSeededMovies_WithoutAuthentication()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/movies?pageSize=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var titles = body.GetProperty("items").EnumerateArray().Select(m => m.GetProperty("title").GetString()).ToList();

        titles.Should().Contain("Інтерстеллар");
    }

    [Fact]
    public async Task GetMovies_FilterByNowShowing_ExcludesComingSoonMovies()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/movies?status=NOW_SHOWING&pageSize=50");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var titles = body.GetProperty("items").EnumerateArray().Select(m => m.GetProperty("title").GetString()).ToList();

        titles.Should().Contain("Оппенгеймер");           // NOW_SHOWING у seed-даних
        titles.Should().NotContain("Дюна: Частина третя"); // COMING_SOON у seed-даних
    }
}
