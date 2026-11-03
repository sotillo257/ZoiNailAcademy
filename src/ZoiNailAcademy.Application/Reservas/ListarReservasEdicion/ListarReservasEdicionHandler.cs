using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Reservas.ListarReservasEdicion;

/// <summary>Who is enrolled in an edition: paid by payment date, then pending by expiry (then expired, cancelled).</summary>
public class ListarReservasEdicionHandler(
    IAppDbContext dbContext, IOcupacionReader ocupacionReader, IReservasSettings settings, TimeProvider timeProvider)
    : IRequestHandler<ListarReservasEdicionQuery, Result<ReservasEdicionDto>>
{
    public async Task<Result<ReservasEdicionDto>> Handle(ListarReservasEdicionQuery request, CancellationToken cancellationToken)
    {
        // Already checked by ListarReservasEdicionValidator in the pipeline.
        _ = IncluirEstados.TryParse(request.Incluir, out var estados);

        var edicion = await dbContext.Ediciones
            .AsNoTracking()
            .Include(e => e.Curso)
            .FirstOrDefaultAsync(e => e.Id == request.EdicionId, cancellationToken)
            ?? throw new NotFoundException("Edición", request.EdicionId);

        var ocupaciones = await ocupacionReader.LeerAsync([edicion.Id], cancellationToken);
        var edicionDto = EdicionAdminDto.From(
            edicion, Ocupacion.De(ocupaciones, edicion.Id), timeProvider.GetUtcNow(), settings.CierreAntesDeInicio);

        var filas = await dbContext.Reservas
            .AsNoTracking()
            .Where(r => r.EdicionId == edicion.Id && estados.Contains(r.Estado))
            .OrderBy(r => r.Estado == EstadoReserva.Pagada ? 0
                : r.Estado == EstadoReserva.Pendiente ? 1
                : r.Estado == EstadoReserva.Expirada ? 2
                : 3)
            .ThenBy(r => r.Estado == EstadoReserva.Pagada ? r.PagadaEn
                : r.Estado == EstadoReserva.Pendiente ? r.ExpiraEn
                : r.Estado == EstadoReserva.Expirada ? r.ExpiradaEn
                : r.CanceladaEn)
            .ThenBy(r => r.CreatedAt)
            .ThenBy(r => r.Id)
            .Select(ReservaAdminConsulta.Proyeccion)
            .ToListAsync(cancellationToken);

        return Result.Success(new ReservasEdicionDto(edicionDto, filas.Select(f => f.ToDto()).ToList()));
    }
}
