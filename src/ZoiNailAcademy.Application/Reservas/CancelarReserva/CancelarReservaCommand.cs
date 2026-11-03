using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Reservas.CancelarReserva;

/// <summary>Body of <c>POST /api/admin/reservas/{id}/cancelar</c>.</summary>
/// <param name="ConfirmarPagada">Required to cancel a paid booking (the refund is done outside the system).</param>
public sealed record CancelarReservaRequest(string Version, string? Motivo, bool ConfirmarPagada = false);

public sealed record CancelarReservaCommand(Guid Id, string Version, string? Motivo, bool ConfirmarPagada)
    : IRequest<Result<ReservaAccionDto>>;
