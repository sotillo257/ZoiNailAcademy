using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Ediciones.ObtenerEdicionAdmin;

public sealed record ObtenerEdicionAdminQuery(Guid Id) : IRequest<Result<EdicionAdminDto>>;
