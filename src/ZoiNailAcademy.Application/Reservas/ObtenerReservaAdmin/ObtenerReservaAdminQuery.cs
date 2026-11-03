using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Reservas.ObtenerReservaAdmin;

public sealed record ObtenerReservaAdminQuery(Guid Id) : IRequest<Result<ReservaAdminDto>>;
