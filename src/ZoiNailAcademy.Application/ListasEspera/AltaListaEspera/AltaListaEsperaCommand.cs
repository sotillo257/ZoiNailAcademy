using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.ListasEspera.AltaListaEspera;

/// <summary>
/// Body of <c>POST /api/lista-espera</c>. The <c>textoConsentimiento</c> the front sends is ignored: the stored
/// text is <see cref="ConsentimientoListaEspera.Vigente"/>.
/// </summary>
/// <param name="Website">Honeypot: hidden in the form, only bots fill it.</param>
public sealed record AltaListaEsperaCommand(
    Guid CursoId, Guid? EdicionPreferidaId, string? FechaPreferida, string Nombre, string Email, string Telefono,
    bool AceptaPrivacidad, string? Website)
    : IRequest<Result>;
