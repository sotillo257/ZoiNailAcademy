using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.ListasEspera.MarcarContactado;

/// <param name="Notas">Absent: the notes stay as they are; null or empty: they are cleared.</param>
public sealed record MarcarContactadoCommand(Guid Id, bool Contactado, Optional<string?> Notas)
    : IRequest<Result<ListaEsperaAdminDto>>;

/// <summary>Body of <c>PUT /api/admin/lista-espera/{id}/contactado</c>.</summary>
public sealed record MarcarContactadoRequest(bool Contactado, Optional<string?> Notas);
