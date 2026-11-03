using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CineVerse.Infrastructure.Persistence;

/// <summary>
/// Design-time factory, потрібна інструменту `dotnet ef` для генерації міграцій
/// без необхідності запускати повний WebApi host.
///
/// Використання (коли на машині розробника встановлено .NET 8 SDK):
///   cd backend
///   dotnet tool install --global dotnet-ef   (якщо ще не встановлено)
///   dotnet ef migrations add InitialCreate --project src/CineVerse.Infrastructure --startup-project src/CineVerse.WebApi
///   dotnet ef database update --project src/CineVerse.Infrastructure --startup-project src/CineVerse.WebApi
///
/// Примітка: у поточному пісочничному середовищі немає доступу до NuGet, тому офіційні
/// файли міграцій EF Core згенерувати тут неможливо. Замість цього еквівалентну схему
/// надано у вигляді SQL-скриптів /database/init/001_schema.sql + 002_seed.sql, які
/// автоматично застосовуються Postgres-контейнером при першому запуску docker-compose.
/// Коли команда матиме SDK локально — перша ж команда `dotnet ef migrations add InitialCreate`
/// згенерує міграцію, яка буде еквівалентна цій SQL-схемі (адже вона побудована на тих самих
/// Fluent API конфігураціях з Persistence/Configurations).
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=cineverse;Username=cineverse;Password=cineverse_dev_password");
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
