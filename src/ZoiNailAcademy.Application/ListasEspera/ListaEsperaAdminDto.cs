using System.Globalization;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.ListasEspera;

/// <param name="EdicionPreferida">Start of the preferred edition in the configured time zone (<c>dd/MM/yyyy HH:mm</c>).</param>
public sealed record ListaEsperaAdminDto(
    Guid Id, Guid CursoId, string Curso, string? EdicionPreferida, string? FechaPreferida, string Nombre, string Email,
    string Telefono, bool Contactado, DateTimeOffset? ContactadoEn, bool Baja, string? Notas, DateTimeOffset CreadoEn)
{
    public const string FormatoFecha = "dd/MM/yyyy HH:mm";

    /// <param name="e">Entry with <c>Curso</c> and <c>EdicionPreferida</c> loaded.</param>
    public static ListaEsperaAdminDto From(ListaEspera e, TimeZoneInfo zona) => new(
        e.Id, e.CursoId, e.Curso.Nombre, FormatearFecha(e.EdicionPreferida?.Inicio, zona), e.FechaPreferida, e.Nombre,
        e.Email, e.Telefono, e.Contactado, e.ContactadoEn, e.BajaEn is not null, e.Notas, e.CreatedAt);

    public static string? FormatearFecha(DateTimeOffset? fecha, TimeZoneInfo zona) =>
        fecha is { } f ? TimeZoneInfo.ConvertTime(f, zona).ToString(FormatoFecha, CultureInfo.InvariantCulture) : null;
}
