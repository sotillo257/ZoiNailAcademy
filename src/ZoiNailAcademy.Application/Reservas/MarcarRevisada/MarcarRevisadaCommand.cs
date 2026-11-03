using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Reservas.MarcarRevisada;

/// <summary>Body of <c>POST /api/admin/reservas/{id}/marcar-revisada</c>.</summary>
public sealed record MarcarRevisadaRequest(string Version, string? Nota);

public sealed record MarcarRevisadaCommand(Guid Id, string Version, string? Nota) : IRequest<Result<ReservaAccionDto>>;
