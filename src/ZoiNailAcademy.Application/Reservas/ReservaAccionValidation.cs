using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.Reservas;

internal static class ReservaAccionValidation
{
    public const string VersionMessage = "La versión de la reserva es obligatoria: recárgala y vuelve a intentarlo.";
    public static readonly string NotaMessage = $"La nota no puede superar {Reserva.NotaInternaMaxLength} caracteres.";
}
