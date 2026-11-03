using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Reservas.ObtenerReservaPublica;

public sealed record ObtenerReservaPublicaQuery(string Codigo) : IRequest<Result<ReservaPublicaDto>>;
