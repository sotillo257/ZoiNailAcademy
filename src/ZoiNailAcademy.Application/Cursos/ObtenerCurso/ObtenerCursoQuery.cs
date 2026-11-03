using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Cursos.ObtenerCurso;

public sealed record ObtenerCursoQuery(string Slug) : IRequest<Result<CursoDetalleDto>>;
