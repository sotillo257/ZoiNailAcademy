using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Reservas.ReservarManual;

public sealed record ReservarManualCommand(
    Guid EdicionId, string Nombre, string Email, string Telefono, bool AceptaCondiciones, string? Website)
    : IRequest<Result<ReservaPublicaDto>>, IReservaFormulario;
