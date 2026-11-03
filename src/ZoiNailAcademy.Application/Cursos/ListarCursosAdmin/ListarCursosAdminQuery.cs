using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Cursos.ListarCursosAdmin;

public sealed record ListarCursosAdminQuery : IRequest<Result<IReadOnlyList<CursoOpcionDto>>>;
