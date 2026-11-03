using MediatR;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Application.Common.Events;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Common.Exceptions;

namespace ZoiNailAcademy.Application.Ediciones.EditarEdicion;

public class EditarEdicionHandler(
    IAppDbContext dbContext, IOcupacionReader ocupacionReader, IPublisher publisher, IReservasSettings settings,
    TimeProvider timeProvider,
    ILogger<EditarEdicionHandler> logger)
    : IRequestHandler<EditarEdicionCommand, Result<EdicionEditadaDto>>
{
    public const string ConcurrenciaMessage =
        "Otra persona ha modificado esta edición mientras la editabas. Recárgala y vuelve a intentarlo.";

    public async Task<Result<EdicionEditadaDto>> Handle(EditarEdicionCommand request, CancellationToken cancellationToken)
    {
        var edicion = await dbContext.Ediciones
            .Include(e => e.Curso)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Edición", request.Id);

        if (!uint.TryParse(request.Version, out var version) || version != edicion.Version)
        {
            throw new ConflictException(ErrorCodes.Concurrencia, ConcurrenciaMessage);
        }

        var ocupaciones = await ocupacionReader.LeerAsync([edicion.Id], cancellationToken);
        var ocupacion = Ocupacion.De(ocupaciones, edicion.Id);
        var now = timeProvider.GetUtcNow();

        var cambio = edicion.Actualizar(
            request.Inicio, request.Plazas, request.Estado, request.Notas, ocupacion.Total,
            request.ConfirmarCambioFecha, now);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // xmin changed between our read and our write: someone else saved the edition first.
            throw new ConflictException(ErrorCodes.Concurrencia, ConcurrenciaMessage);
        }

        if (cambio.Publicada)
        {
            await publisher.PublicarTrasCommitAsync(new EdicionPublicadaEvent(edicion.Id), logger, cancellationToken);
        }

        // TODO F04: when cambio.Cancelada, publish the event that cancels the pending bookings of this edition.

        return Result.Success(new EdicionEditadaDto(
            EdicionAdminDto.From(edicion, ocupacion, now, settings.CierreAntesDeInicio), cambio.Avisos));
    }
}
