using MediatR;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Ediciones.CrearEdicion;

/// <param name="Estado">Defaults to <c>Programada</c>.</param>
/// <param name="Precio">Defaults to the course price.</param>
/// <param name="PrecioReserva">Defaults to the course seat deposit (capped at <paramref name="Precio"/>).</param>
public sealed record CrearEdicionCommand(
    Guid CursoId, DateTimeOffset Inicio, int Plazas, EstadoEdicion? Estado, string? Notas,
    decimal? Precio = null, decimal? PrecioReserva = null)
    : IRequest<Result<EdicionAdminDto>>;
