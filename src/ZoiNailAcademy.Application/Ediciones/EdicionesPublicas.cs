using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Ediciones;

/// <summary>
/// Visible editions for the public web (bookable: scheduled, course active, outside the closing
/// window), loaded for several courses with one query for editions and one for occupancy.
/// </summary>
public class EdicionesPublicas(IAppDbContext dbContext, IOcupacionReader ocupacionReader, IReservasSettings settings, TimeProvider timeProvider)
{
    /// <returns>Visible editions per course id, ordered by <c>Inicio</c>.</returns>
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<EdicionPublicaDto>>> PorCursoAsync(
        IReadOnlyCollection<Guid> cursoIds, CancellationToken cancellationToken)
    {
        // Same rule as Edicion.EsReservable, written as SQL: Inicio - now > cierre ⇔ Inicio > now + cierre.
        var limite = timeProvider.GetUtcNow() + settings.CierreAntesDeInicio;
        var ediciones = await dbContext.Ediciones
            .AsNoTracking()
            .Where(e => cursoIds.Contains(e.CursoId)
                && e.Estado == EstadoEdicion.Programada
                && e.Curso.Activo
                && e.Inicio > limite)
            .OrderBy(e => e.Inicio)
            .ToListAsync(cancellationToken);

        var ocupaciones = await ocupacionReader.LeerAsync(ediciones.Select(e => e.Id).ToList(), cancellationToken);

        return ediciones
            .GroupBy(e => e.CursoId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<EdicionPublicaDto>)g
                    .Select(e => EdicionPublicaDto.From(e, Ocupacion.De(ocupaciones, e.Id), settings.UmbralUltimasPlazas))
                    .ToList());
    }
}
