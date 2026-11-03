using MediatR;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Reservas.ListarReservasEdicion;

/// <param name="Incluir">Comma-separated states (<c>pagadas,pendientes,expiradas,canceladas</c>); empty means paid and pending.</param>
public sealed record ListarReservasEdicionQuery(Guid EdicionId, string? Incluir) : IRequest<Result<ReservasEdicionDto>>;

/// <summary>Response of <c>GET /api/admin/ediciones/{id}/reservas</c>.</summary>
public sealed record ReservasEdicionDto(EdicionAdminDto Edicion, IReadOnlyList<ReservaAdminDto> Reservas);

/// <summary>Parses the <c>incluir</c> filter of the edition bookings.</summary>
public static class IncluirEstados
{
    public static readonly IReadOnlyDictionary<string, EstadoReserva> Valores =
        new Dictionary<string, EstadoReserva>(StringComparer.OrdinalIgnoreCase)
        {
            ["pagadas"] = EstadoReserva.Pagada,
            ["pendientes"] = EstadoReserva.Pendiente,
            ["expiradas"] = EstadoReserva.Expirada,
            ["canceladas"] = EstadoReserva.Cancelada
        };

    /// <summary>The ones that hold a seat.</summary>
    public static readonly IReadOnlySet<EstadoReserva> PorDefecto =
        new HashSet<EstadoReserva> { EstadoReserva.Pagada, EstadoReserva.Pendiente };

    public static bool TryParse(string? incluir, out IReadOnlySet<EstadoReserva> estados)
    {
        estados = PorDefecto;
        var partes = (incluir ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (partes.Length == 0)
        {
            return true;
        }

        var resultado = new HashSet<EstadoReserva>();
        foreach (var parte in partes)
        {
            if (!Valores.TryGetValue(parte, out var estado))
            {
                return false;
            }

            resultado.Add(estado);
        }

        estados = resultado;
        return true;
    }
}
