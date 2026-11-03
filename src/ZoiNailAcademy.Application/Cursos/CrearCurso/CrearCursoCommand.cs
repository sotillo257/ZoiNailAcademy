using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Cursos.CrearCurso;

public sealed record CrearCursoCommand(
    string Nombre, string Slug, string DescripcionCorta, string Descripcion, decimal Precio, string Imagen,
    string? Ubicacion, string? DuracionTexto, bool Activo, int Orden)
    : IRequest<Result<CursoAdminDto>>, ICursoCampos;
