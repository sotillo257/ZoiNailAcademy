using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Application.ListasEspera.ListarListaEspera;

public class ListarListaEsperaHandler(IAppDbContext dbContext, IAppSettings appSettings)
    : IRequestHandler<ListarListaEsperaQuery, Result<PagedResult<ListaEsperaAdminDto>>>
{
    public async Task<Result<PagedResult<ListaEsperaAdminDto>>> Handle(
        ListarListaEsperaQuery request, CancellationToken cancellationToken)
    {
        var query = ListaEsperaConsulta.Filtrar(dbContext.ListaEspera, request.CursoId, request.Contactado, request.Q);
        var total = await query.CountAsync(cancellationToken);
        var entradas = await ListaEsperaConsulta.Ordenar(query)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = entradas.Select(e => ListaEsperaAdminDto.From(e, appSettings.TimeZone)).ToList();
        return Result.Success(new PagedResult<ListaEsperaAdminDto>(items, total, request.Page, request.PageSize));
    }
}
