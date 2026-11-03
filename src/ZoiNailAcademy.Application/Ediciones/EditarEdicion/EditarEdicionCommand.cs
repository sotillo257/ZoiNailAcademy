using MediatR;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Ediciones.EditarEdicion;

/// <param name="Version">The <c>version</c> (xmin) read by the client.</param>
public sealed record EditarEdicionCommand(
    Guid Id, DateTimeOffset Inicio, int Plazas, EstadoEdicion Estado, string? Notas, string Version, bool ConfirmarCambioFecha)
    : IRequest<Result<EdicionEditadaDto>>;

public sealed record EdicionEditadaDto(EdicionAdminDto Edicion, IReadOnlyList<string> Avisos);
