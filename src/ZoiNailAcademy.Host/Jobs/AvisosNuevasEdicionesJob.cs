using MediatR;
using ZoiNailAcademy.Application.AvisosNuevasEdiciones.EnviarAvisos;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Host.Jobs;

/// <summary>
/// Every <c>Avisos:JobIntervaloMinutos</c>, emails the waiting lists of the courses with newly published dates (F11).
/// Safe with several replicas (advisory lock in the command). The timer runs on the injected <see cref="TimeProvider"/>.
/// </summary>
public class AvisosNuevasEdicionesJob(
    IServiceScopeFactory scopeFactory, IAvisosSettings settings, TimeProvider timeProvider, ILogger<AvisosNuevasEdicionesJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(settings.JobIntervalo, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await EjecutarCicloAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
    }

    private async Task EjecutarCicloAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var result = await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new EnviarAvisosNuevasEdicionesCommand(), stoppingToken);
            if (result.Value > 0)
            {
                logger.LogInformation("Avisos de nuevas fechas en este ciclo: {Avisados}", result.Value);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Never let one failing cycle stop the job.
            logger.LogError(ex, "Falló el ciclo de avisos de nuevas fechas.");
        }
    }
}
