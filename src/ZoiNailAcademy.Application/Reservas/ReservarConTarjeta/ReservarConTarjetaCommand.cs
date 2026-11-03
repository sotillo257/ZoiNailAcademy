using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Reservas.ReservarConTarjeta;

public sealed record ReservarConTarjetaCommand(
    Guid EdicionId, string Nombre, string Email, string Telefono, bool AceptaCondiciones, string? Website)
    : IRequest<Result<ReservaTarjetaCreadaDto>>, IReservaFormulario;

/// <param name="CheckoutUrl">Stripe Checkout page the front redirects to.</param>
public sealed record ReservaTarjetaCreadaDto(string Codigo, string CheckoutUrl);
