using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Cursos.EditarCurso;

public sealed record EditarCursoCommand(
    Guid Id, string Nombre, string Slug, string DescripcionCorta, string Descripcion, decimal Precio, string Imagen,
    string? Ubicacion, string? DuracionTexto, bool Activo, int Orden)
    : IRequest<Result<CursoEditadoDto>>, ICursoCampos;

public sealed record CursoEditadoDto(CursoAdminDto Curso, IReadOnlyList<string> Avisos);
