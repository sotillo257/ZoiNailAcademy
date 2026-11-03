using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Emails.Plantillas;
using ZoiNailAcademy.Application.ListasEspera;

namespace ZoiNailAcademy.Application.Emails.NotificarEventos;

/// <summary>
/// Someone joined the waiting list: notice to the client. A re-sign-up of the same person is notified again,
/// so the email has no reference (one event per request, never deduplicated by type).
/// </summary>
public class ListaEsperaEmailHandler(
    IAppDbContext dbContext, BandejaSalida bandeja, ILogger<ListaEsperaEmailHandler> logger)
    : INotificationHandler<ListaEsperaAltaEvent>
{
    public async Task Handle(ListaEsperaAltaEvent notification, CancellationToken cancellationToken)
    {
        var entrada = await dbContext.ListaEspera.AsNoTracking()
            .Include(e => e.Curso).Include(e => e.EdicionPreferida)
            .FirstOrDefaultAsync(e => e.Id == notification.ListaEsperaId, cancellationToken);
        if (entrada is null)
        {
            logger.LogWarning("No existe la entrada {ListaEsperaId} de la lista de espera.", notification.ListaEsperaId);
            return;
        }

        var modelo = new ListaEsperaEmailModelo(
            entrada.Curso.Nombre, entrada.Nombre, entrada.Email, entrada.Telefono, entrada.FechaPreferida,
            entrada.EdicionPreferida?.Inicio);
        await bandeja.EncolarParaClientaAsync(
            TiposEmail.ListaEsperaClienta, ListaEsperaClientaEmail.Crear(modelo, bandeja.Marca()), null, cancellationToken);
    }
}
