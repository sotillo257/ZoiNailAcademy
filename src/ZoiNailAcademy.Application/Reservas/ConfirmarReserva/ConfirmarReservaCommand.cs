using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Reservas.ConfirmarReserva;

/// <summary>Body of <c>POST /api/admin/reservas/{id}/confirmar</c>.</summary>
public sealed record ConfirmarReservaRequest(string Version, string? Nota);

public sealed record ConfirmarReservaCommand(Guid Id, string Version, string? Nota) : IRequest<Result<ReservaAccionDto>>;
