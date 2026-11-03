using MediatR;
using ZoiNailAcademy.Application.Emails.EnviarEmailsPendientes;
using ZoiNailAcademy.Application.Emails.LimpiarEmailsEnviados;

namespace ZoiNailAcademy.Host.Jobs;

/// <summary>
/// Sends the outbox every <see cref="Intervalo"/> and, once a day, deletes old sent emails. Safe with several
/// replicas (advisory lock in the command). The timer runs on the injected <see cref="TimeProvider"/>.
/// </summary>
public class EnviarEmailsJob(IServiceScopeFactory scopeFactory, TimeProvider timeProvider, ILogger<EnviarEmailsJob> logger)
    : BackgroundService
{
    public static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan IntervaloLimpieza = TimeSpan.FromDays(1);

    private DateTimeOffset? _ultimaLimpieza;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await EjecutarAsync(new EnviarEmailsPendientesCommand(), "envío de emails", stoppingToken);
                await LimpiarSiTocaAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
    }

    private async Task LimpiarSiTocaAsync(CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();
        if (_ultimaLimpieza is { } ultima && now - ultima < IntervaloLimpieza)
        {
            return;
        }

        _ultimaLimpieza = now;
        await EjecutarAsync(new LimpiarEmailsEnviadosCommand(), "limpieza de emails enviados", stoppingToken);
    }

    private async Task EjecutarAsync(IRequest<Application.Common.Result<int>> command, string nombre, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(command, stoppingToken);
            if (result.Value > 0)
            {
                logger.LogInformation("Ciclo de {Nombre}: {Cantidad}", nombre, result.Value);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falló el ciclo de {Nombre}.", nombre);
        }
    }
}
