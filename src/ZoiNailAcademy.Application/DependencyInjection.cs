using FluentValidation;
using ZoiNailAcademy.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ZoiNailAcademy.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<Authentication.AuthTokenIssuer>();
        services.AddScoped<Common.Interfaces.IOcupacionReader, Ediciones.OcupacionReader>();
        services.AddScoped<Ediciones.EdicionesPublicas>();
        services.AddScoped<Reservas.CrearReservaService>();
        services.AddScoped<Reservas.ReservaPublicaDtoFactory>();
        services.AddScoped<Emails.BandejaSalida>();

        return services;
    }
}
