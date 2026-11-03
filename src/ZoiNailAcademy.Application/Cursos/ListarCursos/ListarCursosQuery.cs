using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Cursos.ListarCursos;

public sealed record ListarCursosQuery : IRequest<Result<IReadOnlyList<CursoResumenDto>>>;
