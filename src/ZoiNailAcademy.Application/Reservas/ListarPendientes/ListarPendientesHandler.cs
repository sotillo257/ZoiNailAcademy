using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Domain.Services;

namespace ZoiNailAcademy.Application.Reservas.ListarPendientes;

/// <summary>
/// What the client has to chase: bookings that need review, valid pending ones (the soonest to expire first) and
/// manual ones expired in the last 7 days (the most recent first), each expired one with whether a seat is free.
/// </summary>
public class ListarPendientesHandler(IAppDbContext dbContext, IOcupacionReader ocupacionReader, TimeProvider timeProvider)
    : IRequestHandler<ListarPendientesQuery, Result<IReadOnlyList<ReservaAdminDto>>>
{
    public async Task<Result<IReadOnlyList<ReservaAdminDto>>> Handle(
        ListarPendientesQuery request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var desde = now - ListarPendientesQuery.VentanaExpiradas;

        var query = dbContext.Reservas.AsNoTracking().Where(r =>
            r.RequiereRevision
            || (r.Estado == EstadoReserva.Pendiente && r.ExpiraEn > now)
            || (r.Estado == EstadoReserva.Expirada && r.Metodo == MetodoPago.Manual && r.ExpiradaEn >= desde));

        if (request.Metodo is { } metodo)
        {
            query = query.Where(r => r.Metodo == metodo);
        }

        var filas = await ReservaAdminConsulta.Buscar(query, request.Q)
            .OrderBy(r => r.RequiereRevision ? 0 : r.Estado == EstadoReserva.Pendiente ? 1 : 2)
            .ThenBy(r => r.RequiereRevision ? r.CreatedAt : (DateTimeOffset?)null)
            .ThenBy(r => !r.RequiereRevision && r.Estado == EstadoReserva.Pendiente ? r.ExpiraEn : (DateTimeOffset?)null)
            .ThenByDescending(r => !r.RequiereRevision && r.Estado != EstadoReserva.Pendiente ? r.ExpiradaEn : null)
            .ThenBy(r => r.Id)
            .Select(ReservaAdminConsulta.Proyeccion)
            .ToListAsync(cancellationToken);

        // Free seats only matter for expired bookings: one grouped query for all their editions.
        var expiradas = filas.Where(f => f.Estado == EstadoReserva.Expirada).Select(f => f.EdicionId).Distinct().ToList();
        var ocupaciones = await ocupacionReader.LeerAsync(expiradas, cancellationToken);

        IReadOnlyList<ReservaAdminDto> dtos = filas
            .Select(f => f.ToDto(f.Estado == EstadoReserva.Expirada
                ? PlazasCalculator.Libres(f.Plazas, Ocupacion.De(ocupaciones, f.EdicionId).Total) > 0
                : null))
            .ToList();

        return Result.Success(dtos);
    }
}
