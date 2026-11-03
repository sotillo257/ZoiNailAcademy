using MediatR;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Application.Common.Events;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Ediciones.CrearEdicion;

public class CrearEdicionHandler(
    IAppDbContext dbContext, IPublisher publisher, IReservasSettings settings, TimeProvider timeProvider,
    ILogger<CrearEdicionHandler> logger)
    : IRequestHandler<CrearEdicionCommand, Result<EdicionAdminDto>>
{
    public async Task<Result<EdicionAdminDto>> Handle(CrearEdicionCommand request, CancellationToken cancellationToken)
    {
        var curso = await dbContext.Cursos.FirstOrDefaultAsync(c => c.Id == request.CursoId, cancellationToken)
            ?? throw new NotFoundException("Curso", request.CursoId);

        var now = timeProvider.GetUtcNow();
        var edicion = Edicion.Create(
            curso, request.Inicio, request.Plazas, request.Estado ?? EstadoEdicion.Programada, request.Notas, now);

        dbContext.Ediciones.Add(edicion);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (edicion.Estado == EstadoEdicion.Programada)
        {
            await publisher.PublicarTrasCommitAsync(new EdicionPublicadaEvent(edicion.Id), logger, cancellationToken);
        }

        return Result.Success(EdicionAdminDto.From(edicion, Ocupacion.Ninguna, now, settings.CierreAntesDeInicio));
    }
}
