using MediatR;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Reservas.ExpirarReservasCaducadas;

namespace ZoiNailAcademy.Host.Jobs;

/// <summary>
/// Expires unpaid bookings every <c>Reservas:JobIntervaloMinutos</c>. Seats are already free by time; this keeps
/// the states coherent and triggers the emails. Safe with several replicas (advisory lock in the command).
/// The timer runs on the injected <see cref="TimeProvider"/>, so a frozen fake clock in tests never fires it.
/// </summary>
public class ExpirarReservasJob(
    IServiceScopeFactory scopeFactory, IReservasSettings settings, TimeProvider timeProvider, ILogger<ExpirarReservasJob> logger)
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
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var result = await sender.Send(new ExpirarReservasCaducadasCommand(), stoppingToken);
            if (result.Value > 0)
            {
                logger.LogInformation("Reservas expiradas en este ciclo: {Expiradas}", result.Value);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Never let one failing cycle stop the job.
            logger.LogError(ex, "Falló el ciclo de caducidad de reservas.");
        }
    }
}
