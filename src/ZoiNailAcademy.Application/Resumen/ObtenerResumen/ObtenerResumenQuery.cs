using MediatR;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Ediciones;

namespace ZoiNailAcademy.Application.Resumen.ObtenerResumen;

public sealed record ObtenerResumenQuery : IRequest<Result<ResumenDto>>
{
    public const int ProximasEdiciones = 5;
}

/// <summary>What the client has to do today (home page of the backoffice).</summary>
/// <param name="PendientesDePago">Manual bookings pending and not expired.</param>
/// <param name="PendientesQueCaducanHoy">Of those, the ones that expire before midnight in <c>App:TimeZone</c>.</param>
/// <param name="RequierenRevision">Payments that arrived without a seat and are not marked as reviewed.</param>
/// <param name="ListaEsperaSinContactar">Waiting-list entries not contacted and not unsubscribed.</param>
/// <param name="ProximasEdiciones">The next editions that have not started and are not cancelled.</param>
public sealed record ResumenDto(
    int PendientesDePago, int PendientesQueCaducanHoy, int RequierenRevision, int ListaEsperaSinContactar,
    IReadOnlyList<EdicionAdminDto> ProximasEdiciones);
