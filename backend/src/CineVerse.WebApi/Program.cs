using System.Text;
using CineVerse.Application;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Infrastructure;
using CineVerse.Infrastructure.Persistence;
using CineVerse.WebApi.Hubs;
using CineVerse.WebApi.Middleware;
using CineVerse.WebApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// Serilog замінює стандартний ILogger застосунку (п. "бажано" аудиту — пакет Serilog.AspNetCore
// був підключений ще з Етапу 1, але жодного разу не увімкнений). Консоль — для docker compose logs,
// файл з ротацією по днях — для довшого зберігання на самому сервері.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/cineverse-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .CreateLogger();

try
{
    Log.Information("Запуск CineVerse.WebApi...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // --- Реєстрація шарів Clean Architecture ---
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // --- Веб-шар ---
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ISeatStatusNotifier, SignalRSeatStatusNotifier>();
builder.Services.AddSignalR();

builder.Services.AddSwaggerGen(options =>
{
    // Дозволяє тестувати захищені ендпоінти прямо зі Swagger UI (кнопка "Authorize")
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Введіть: {ваш access-токен}"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("CineVerseFrontend", policy =>
    {
        policy.WithOrigins(builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// --- JWT Bearer автентифікація (Етап 3) ---
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    // Порожнє значення (стан appsettings.json "з коробки") не повинно тихо пройти далі —
    // інакше застосунок піднявся б і почав підписувати токени порожнім секретом.
    throw new InvalidOperationException(
        "Jwt:Secret не налаштовано. Задайте його через user-secrets (розробка) або змінну середовища Jwt__Secret (продакшн/Docker).");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSection["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30) // мінімальний допуск розсинхронізації годинників замість дефолтних 5 хв
    };
});

// --- Авторизація за ролями (п.2.2 ТЗ: CUSTOMER, CASHIER, MANAGER, ADMIN, CHECKER) ---
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"))
    .AddPolicy("StaffOnly", policy => policy.RequireRole("ADMIN", "MANAGER", "CASHIER"))
    .AddPolicy("CheckerOnly", policy => policy.RequireRole("ADMIN", "CHECKER"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging(); // логує кожен HTTP-запит: метод, шлях, статус, тривалість

// UseHttpsRedirection() навмисно НЕ використовується: TLS термінується на окремому reverse-proxy
// (сервіс nginx у docker-compose.yml), а сам контейнер backend слухає лише HTTP на 8080.
// Раніше цей виклик тут був — без налаштованого HTTPS-порту в самому контейнері він або
// шумів попередженнями в лог при кожному запиті, або міг зламати редірект, залежно від хоста.

app.UseCors("CineVerseFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<HallHub>("/hubs/hall"); // Real-time оновлення статусів місць у залі

// Наразі схема БД піднімається SQL init-скриптами (database/init/). Коли з'являться
// справжні EF Core міграції, цей виклик підхопить їх автоматично при старті.
await app.Services.MigrateDatabaseAsync();

app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException) // HostAbortedException — штатна поведінка `dotnet ef`/дизайн-тайм команд, не помилка
{
    Log.Fatal(ex, "CineVerse.WebApi аварійно завершився під час старту.");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { } // для WebApplicationFactory в інтеграційних тестах (Етап 9)
