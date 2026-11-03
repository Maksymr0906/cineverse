using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace CineVerse.IntegrationTests.Common;

/// <summary>
/// Піднімає РЕАЛЬНИЙ PostgreSQL у Docker-контейнері (Testcontainers) саме з тими SQL-скриптами
/// схеми/seed-даних, що й docker-compose.yml (database/init/*.sql), — тому інтеграційні тести
/// перевіряють точно те середовище, яке отримає й розробник при `docker compose up`.
///
/// ВАЖЛИВО: ці тести потребують працюючого Docker-демона на машині, де вони запускаються
/// (`dotnet test` підніме і знищить контейнер автоматично). У пісочниці, де писався цей каркас,
/// немає доступу до Docker-демона, тож виконати `dotnet test` і показати "зелений" прогін тут
/// неможливо — код написано за перевіреним, стандартним патерном Testcontainers + WebApplicationFactory
/// (той самий підхід, що в офіційній документації testcontainers.com), і запрацює "з коробки"
/// на машині з Docker.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;

    public CustomWebApplicationFactory()
    {
        var initScriptsPath = ResolveDatabaseInitPath();

        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("cineverse_test")
            .WithUsername("cineverse")
            .WithPassword("cineverse_test_password")
            // Ті самі init-скрипти, що й у docker-compose.yml — жодного дублювання схеми в тестах.
            .WithResourceMapping(Path.Combine(initScriptsPath, "001_schema.sql"), "/docker-entrypoint-initdb.d/001_schema.sql")
            .WithResourceMapping(Path.Combine(initScriptsPath, "002_seed.sql"), "/docker-entrypoint-initdb.d/002_seed.sql")
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSql"] = _postgres.GetConnectionString(),
                // Redis у цих тестах не зачіпається (Health/Auth/Catalog не викликають ISeatLockService),
                // тому підключення навмисно вказує на недоступний хост — якщо якийсь тест випадково
                // зачепить Redis-залежний код, він одразу впаде з очевидною помилкою, а не зависне.
                ["ConnectionStrings:Redis"] = "localhost:1",
                ["Jwt:Secret"] = "integration_test_secret_key_at_least_32_chars_long"
            });
        });
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync() => await _postgres.StopAsync();

    private static string ResolveDatabaseInitPath()
    {
        // tests/CineVerse.IntegrationTests/bin/Debug/net8.0 → підіймаємось до кореня репозиторію → database/init
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "database", "init")))
        {
            dir = dir.Parent;
        }

        return dir is null
            ? throw new DirectoryNotFoundException("Не вдалося знайти папку database/init відносно тестової збірки.")
            : Path.Combine(dir.FullName, "database", "init");
    }
}
