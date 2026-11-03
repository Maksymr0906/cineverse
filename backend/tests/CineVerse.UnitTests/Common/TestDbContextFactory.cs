using CineVerse.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.UnitTests.Common;

/// <summary>
/// Створює ApplicationDbContext поверх SQLite ":memory:" замість справжнього PostgreSQL —
/// це дозволяє тестувати хендлери (CQRS-команди/запити) без Docker/мережі, зберігаючи
/// РЕАЛЬНУ реляційну поведінку (зовнішні ключі, транзакції, атомарний ExecuteUpdateAsync).
///
/// Свідомо НЕ використовуємо провайдер EF Core InMemory: Microsoft прямо попереджає, що він
/// не повторює поведінку реальної СУБД (не перевіряє FK/unique-обмеження так само, як реляційна
/// БД, і — критично для наших тестів ValidateTicketCommandHandler — не підтримує
/// ExecuteUpdateAsync/ExecuteDeleteAsync). SQLite в оперативній памʼяті — набагато ближчий
/// до PostgreSQL за семантикою, лишаючись таким же швидким і незалежним від зовнішніх сервісів.
///
/// SQLite-з'єднання ":memory:" живе, доки відкрите хоча б одне з'єднання — тому обгортка
/// повертає IDisposable, який тримає SqliteConnection відкритим на час тесту.
/// </summary>
public sealed class TestDbContext : IDisposable
{
    private readonly SqliteConnection _connection;
    public ApplicationDbContext Context { get; }

    public TestDbContext()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new ApplicationDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
