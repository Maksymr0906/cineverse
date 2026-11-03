using CineVerse.Application.Common.Interfaces;
using CineVerse.Infrastructure.BackgroundServices;
using CineVerse.Infrastructure.Persistence;
using CineVerse.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CineVerse.Infrastructure;

/// <summary>Реєстрація сервісів Infrastructure-шару: EF Core + PostgreSQL, Redis, зовнішні інтеграції.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PostgreSql")));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));

        // --- Auth (Етап 3) ---
        services.AddSingleton<IDateTime, SystemDateTime>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailService, EmailService>();

        // --- Бронювання у реальному часі (Етап 5) ---
        services.AddSingleton<ISeatLockService, RedisSeatLockService>();
        services.AddHostedService<ExpiredBookingWorker>();

        // --- Оплата та квитки (Етап 6) ---
        services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();
        services.AddScoped<IPaymentGatewayService, MockPaymentGatewayService>();

        return services;
    }
}
