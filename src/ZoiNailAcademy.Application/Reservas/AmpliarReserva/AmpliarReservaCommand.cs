using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Reservas.AmpliarReserva;

/// <summary>Body of <c>POST /api/admin/reservas/{id}/ampliar</c>.</summary>
public sealed record AmpliarReservaRequest(string Version, int Horas);

public sealed record AmpliarReservaCommand(Guid Id, string Version, int Horas) : IRequest<Result<ReservaAccionDto>>;
