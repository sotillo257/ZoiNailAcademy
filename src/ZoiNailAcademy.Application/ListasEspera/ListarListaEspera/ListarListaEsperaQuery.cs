using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.ListasEspera.ListarListaEspera;

/// <param name="Q">Contained in the name, email or phone, ignoring case and accents.</param>
public sealed record ListarListaEsperaQuery(
    Guid? CursoId, bool? Contactado, string? Q, int Page = ListarListaEsperaQuery.PrimeraPagina,
    int PageSize = ListarListaEsperaQuery.PageSizePorDefecto)
    : IRequest<Result<PagedResult<ListaEsperaAdminDto>>>
{
    public const int PrimeraPagina = 1;
    public const int PageSizePorDefecto = 50;
    public const int PageSizeMaximo = 200;
}
