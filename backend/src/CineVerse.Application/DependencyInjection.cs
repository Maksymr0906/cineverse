using System.Reflection;
using CineVerse.Application.Common.Behaviours;
using CineVerse.Application.Features.Tickets.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CineVerse.Application;

/// <summary>Реєстрація сервісів Application-шару: MediatR (CQRS), FluentValidation.
/// (AutoMapper було прибрано після аудиту — DTO в проєкті завжди збираються вручну через
/// конструктори record'ів у хендлерах, пакет був підключений, але жодного разу не використаний.)</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });
        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<TicketIssuanceService>();

        return services;
    }
}
