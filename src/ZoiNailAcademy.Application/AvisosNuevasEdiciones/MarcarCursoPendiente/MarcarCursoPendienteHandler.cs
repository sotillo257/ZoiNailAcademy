using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.AvisosNuevasEdiciones.MarcarCursoPendiente;

/// <summary>
/// An edition was published: marks its course as pending notice (<see cref="AvisoPendiente"/>). Nothing is enqueued
/// here: <c>AvisosNuevasEdicionesJob</c> groups every date published before it runs into one email per person.
/// </summary>
public class MarcarCursoPendienteHandler(
    IAppDbContext dbContext, TimeProvider timeProvider, ILogger<MarcarCursoPendienteHandler> logger)
    : INotificationHandler<EdicionPublicadaEvent>
{
    private const int MaxIntentos = 3;

    public async Task Handle(EdicionPublicadaEvent notification, CancellationToken cancellationToken)
    {
        var cursoId = await dbContext.Ediciones
            .Where(e => e.Id == notification.EdicionId)
            .Select(e => (Guid?)e.CursoId)
            .FirstOrDefaultAsync(cancellationToken);
        if (cursoId is not { } id)
        {
            logger.LogWarning("No existe la edición publicada {EdicionId}; no se marca el aviso.", notification.EdicionId);
            return;
        }

        for (var intento = 1; ; intento++)
        {
            var pendiente = await dbContext.AvisosPendientes.FirstOrDefaultAsync(p => p.CursoId == id, cancellationToken);
            if (pendiente is null)
            {
                pendiente = AvisoPendiente.Create(id, timeProvider.GetUtcNow());
                dbContext.AvisosPendientes.Add(pendiente);
            }
            else
            {
                pendiente.RegistrarPublicacion();
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateException ex) when (intento < MaxIntentos)
            {
                // Another publication created the mark, or the job removed it, at the same time: read it again.
                logger.LogInformation(ex, "El aviso pendiente del curso {CursoId} cambió a la vez; se reintenta.", id);
                dbContext.AvisosPendientes.Entry(pendiente).State = EntityState.Detached;
            }
        }
    }
}
