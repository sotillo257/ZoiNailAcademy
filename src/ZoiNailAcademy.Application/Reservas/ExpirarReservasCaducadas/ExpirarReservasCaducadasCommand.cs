using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Reservas.ExpirarReservasCaducadas;

/// <summary>Sent by the expiry job every few minutes. Returns how many bookings were expired.</summary>
public sealed record ExpirarReservasCaducadasCommand : IRequest<Result<int>>;
