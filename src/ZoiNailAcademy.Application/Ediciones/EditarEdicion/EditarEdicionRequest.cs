using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Ediciones.EditarEdicion;

/// <summary>Body of <c>PUT /api/admin/ediciones/{id}</c>.</summary>
public sealed record EditarEdicionRequest(
    DateTimeOffset Inicio, int Plazas, EstadoEdicion Estado, string? Notas, string Version, bool ConfirmarCambioFecha = false);
