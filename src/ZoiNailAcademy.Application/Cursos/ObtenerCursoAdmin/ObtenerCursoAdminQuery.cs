using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Cursos.ObtenerCursoAdmin;

/// <summary>Every field of a course, active or not, for the edit screen of the backoffice.</summary>
public sealed record ObtenerCursoAdminQuery(Guid Id) : IRequest<Result<CursoAdminDto>>;
