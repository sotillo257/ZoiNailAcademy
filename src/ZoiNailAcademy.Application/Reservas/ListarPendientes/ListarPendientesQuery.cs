using MediatR;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Reservas.ListarPendientes;

/// <param name="Q">A booking code however it is typed, or text in name, email or phone.</param>
public sealed record ListarPendientesQuery(string? Q, MetodoPago? Metodo) : IRequest<Result<IReadOnlyList<ReservaAdminDto>>>
{
    /// <summary>Expired manual bookings stay on the list this long (the receipt may arrive late).</summary>
    public static readonly TimeSpan VentanaExpiradas = TimeSpan.FromDays(7);

    public const int QMaxLength = 200;
}
