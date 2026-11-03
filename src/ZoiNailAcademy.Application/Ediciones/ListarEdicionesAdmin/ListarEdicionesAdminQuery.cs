using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Ediciones.ListarEdicionesAdmin;

public sealed record ListarEdicionesAdminQuery(Guid? CursoId, bool IncluirPasadas)
    : IRequest<Result<IReadOnlyList<EdicionAdminDto>>>;
