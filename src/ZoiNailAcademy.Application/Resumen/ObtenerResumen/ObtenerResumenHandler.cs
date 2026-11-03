using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Resumen.ObtenerResumen;

public class ObtenerResumenHandler(
    IAppDbContext dbContext,
    IOcupacionReader ocupacionReader,
    IReservasSettings reservasSettings,
    IAppSettings appSettings,
    TimeProvider timeProvider)
    : IRequestHandler<ObtenerResumenQuery, Result<ResumenDto>>
{
    public async Task<Result<ResumenDto>> Handle(ObtenerResumenQuery request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var finDelDia = ZonaHoraria.FinDelDia(now, appSettings.TimeZone);

        var pendientes = dbContext.Reservas.Where(r =>
            r.Metodo == MetodoPago.Manual && r.Estado == EstadoReserva.Pendiente && r.ExpiraEn > now);
        var pendientesDePago = await pendientes.CountAsync(cancellationToken);
        var caducanHoy = await pendientes.CountAsync(r => r.ExpiraEn < finDelDia, cancellationToken);
        var requierenRevision = await dbContext.Reservas.CountAsync(r => r.RequiereRevision, cancellationToken);
        var sinContactar = await dbContext.ListaEspera.CountAsync(e => !e.Contactado && e.BajaEn == null, cancellationToken);

        var ediciones = await dbContext.Ediciones
            .AsNoTracking()
            .Include(e => e.Curso)
            .Where(e => e.Inicio > now && e.Estado != EstadoEdicion.Cancelada)
            .OrderBy(e => e.Inicio).ThenBy(e => e.Id)
            .Take(ObtenerResumenQuery.ProximasEdiciones)
            .ToListAsync(cancellationToken);
        var ocupaciones = await ocupacionReader.LeerAsync(ediciones.Select(e => e.Id).ToList(), cancellationToken);
        var proximas = ediciones
            .Select(e => EdicionAdminDto.From(e, Ocupacion.De(ocupaciones, e.Id), now, reservasSettings.CierreAntesDeInicio))
            .ToList();

        return Result.Success(new ResumenDto(pendientesDePago, caducanHoy, requierenRevision, sinContactar, proximas));
    }
}
