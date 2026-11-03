using MediatR;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Ediciones.CrearEdicion;

/// <param name="Estado">Defaults to <c>Programada</c>.</param>
public sealed record CrearEdicionCommand(Guid CursoId, DateTimeOffset Inicio, int Plazas, EstadoEdicion? Estado, string? Notas)
    : IRequest<Result<EdicionAdminDto>>;
