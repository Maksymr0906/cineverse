using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CineVerse.Infrastructure.Persistence;

/// <summary>
/// Застосовує міграції при старті застосунку (production/staging).
///
/// ВАЖЛИВО: наразі в проєкті ще немає жодної згенерованої EF Core міграції (див. коментар
/// у ApplicationDbContextFactory.cs — причина у відсутності доступу до NuGet у середовищі
/// розробки цього каркасу). Це означає, що GetPendingMigrationsAsync() завжди повертатиме
/// порожній список — НЕ тому, що схема вже актуальна, а тому, що самих міграцій ще не існує.
/// Тому нижче явно логується це попередження, а не мовчазний no-op: інакше легко втратити
/// годину на діагностику "чому немає таблиці Users", хоча причина — том Postgres, ініціалізований
/// ДО того, як з'явились database/init/*.sql (init-скрипти Postgres виконує лише один раз,
/// при створенні порожнього тому). Рішення: `docker compose down -v && docker compose up -d --build`.
///
/// Коли з'являться реальні файли міграцій (dotnet ef migrations add ...), ця логіка запрацює
/// "по-справжньому" і жодних змін тут вносити не доведеться.
/// </summary>
public static class DbInitializer
{
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        try
        {
            var appliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            var allMigrations = context.Database.GetMigrations().ToList();

            if (allMigrations.Count == 0)
            {
                logger.LogWarning(
                    "У проєкті ще немає жодної EF Core міграції — схема БД керується SQL-скриптами " +
                    "database/init/*.sql, які Postgres виконує лише один раз при створенні ПОРОЖНЬОГО тому. " +
                    "Якщо бачите помилку на кшталт 'relation \"Users\" does not exist' — " +
                    "том Postgres, найімовірніше, було створено ДО появи цих скриптів. " +
                    "Виправлення: docker compose down -v && docker compose up -d --build " +
                    "(увага: -v видаляє дані в цьому томі).");
                return;
            }

            var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count > 0)
            {
                logger.LogInformation("Застосовуємо {Count} відкладених EF Core міграцій: {Migrations}",
                    pending.Count, string.Join(", ", pending));
                await context.Database.MigrateAsync();
            }
            else
            {
                logger.LogInformation("Схема БД актуальна: застосовано {Count} міграцій, відкладених немає.", appliedMigrations.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не вдалося перевірити/застосувати міграції EF Core. Перевірте доступність PostgreSQL та рядок підключення.");
        }
    }
}
