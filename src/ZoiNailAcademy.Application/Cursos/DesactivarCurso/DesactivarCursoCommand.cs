using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Cursos.DesactivarCurso;

/// <summary><c>DELETE /api/admin/cursos/{id}</c>: courses are deactivated, never deleted.</summary>
public sealed record DesactivarCursoCommand(Guid Id) : IRequest<Result>;
