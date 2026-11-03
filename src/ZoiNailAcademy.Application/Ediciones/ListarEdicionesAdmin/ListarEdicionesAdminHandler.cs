using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Application.Ediciones.ListarEdicionesAdmin;

public class ListarEdicionesAdminHandler(
    IAppDbContext dbContext, IOcupacionReader ocupacionReader, IReservasSettings settings, TimeProvider timeProvider)
    : IRequestHandler<ListarEdicionesAdminQuery, Result<IReadOnlyList<EdicionAdminDto>>>
{
    public async Task<Result<IReadOnlyList<EdicionAdminDto>>> Handle(
        ListarEdicionesAdminQuery request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var query = dbContext.Ediciones.AsNoTracking().Include(e => e.Curso).AsQueryable();
        if (request.CursoId is { } cursoId)
        {
            query = query.Where(e => e.CursoId == cursoId);
        }

        if (!request.IncluirPasadas)
        {
            query = query.Where(e => e.Inicio > now);
        }

        var ediciones = await query.ToListAsync(cancellationToken);
        var ocupaciones = await ocupacionReader.LeerAsync(ediciones.Select(e => e.Id).ToList(), cancellationToken);

        // Upcoming first (soonest first), then past ones (most recent first).
        IReadOnlyList<EdicionAdminDto> dtos = ediciones
            .OrderBy(e => e.HaEmpezado(now))
            .ThenBy(e => e.HaEmpezado(now) ? -e.Inicio.UtcTicks : e.Inicio.UtcTicks)
            .Select(e => EdicionAdminDto.From(e, Ocupacion.De(ocupaciones, e.Id), now, settings.CierreAntesDeInicio))
            .ToList();

        return Result.Success(dtos);
    }
}
