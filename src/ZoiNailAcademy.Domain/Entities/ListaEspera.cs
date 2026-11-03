using System.Buffers.Text;
using System.Security.Cryptography;
using ZoiNailAcademy.Domain.Common;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Services;

namespace ZoiNailAcademy.Domain.Entities;

/// <summary>
/// A person waiting for new dates of a course (one entry per course and email). The client calls them
/// when she opens dates; F11 emails them with the consent stored here and lets them unsubscribe with
/// <see cref="BajaToken"/>.
/// </summary>
public class ListaEspera : BaseAuditableEntity
{
    public const int NombreMaxLength = 120;
    public const int EmailMaxLength = 200;
    public const int TelefonoMaxLength = 20;
    public const int FechaPreferidaMaxLength = 200;
    public const int TextoConsentimientoMaxLength = 500;
    public const int NotasMaxLength = 500;
    public const int BajaTokenMaxLength = 64;
    public const int BusquedaMaxLength = NombreMaxLength + EmailMaxLength + TelefonoMaxLength + 2;

    private const int BajaTokenBytes = 32;

    public Guid CursoId { get; private set; }
    public Curso Curso { get; private set; } = null!;

    /// <summary>Edition the person wanted when it was full (optional).</summary>
    public Guid? EdicionPreferidaId { get; private set; }
    public Edicion? EdicionPreferida { get; private set; }

    /// <summary>Free text: "fines de semana", "en marzo"…</summary>
    public string? FechaPreferida { get; private set; }

    public string Nombre { get; private set; } = null!;

    /// <summary>Trimmed and lower-cased.</summary>
    public string Email { get; private set; } = null!;

    /// <summary>E.164, e.g. <c>+34612345678</c>.</summary>
    public string Telefono { get; private set; } = null!;

    /// <summary>When the person ticked the consent box.</summary>
    public DateTimeOffset ConsentimientoEn { get; private set; }

    /// <summary>Literal copy of the accepted text (GDPR proof of consent).</summary>
    public string TextoConsentimiento { get; private set; } = null!;

    public bool Contactado { get; private set; }
    public DateTimeOffset? ContactadoEn { get; private set; }

    /// <summary>Internal notes of the client.</summary>
    public string? Notas { get; private set; }

    /// <summary>Random unsubscribe token (32 bytes, base64url), unique.</summary>
    public string BajaToken { get; private set; } = null!;

    public DateTimeOffset? BajaEn { get; private set; }

    /// <summary>Name, email and phone normalized with <see cref="TextoBusqueda"/> (search without case or accents).</summary>
    public string Busqueda { get; private set; } = null!;

    private ListaEspera()
    {
    }

    /// <param name="edicionPreferida">Must belong to <paramref name="curso"/>.</param>
    public static ListaEspera Create(
        Curso curso, Edicion? edicionPreferida, string? fechaPreferida, string nombre, string email, string telefono,
        string textoConsentimiento, DateTimeOffset now)
    {
        var entrada = new ListaEspera
        {
            Id = Guid.NewGuid(),
            Curso = curso,
            CursoId = curso.Id,
            BajaToken = GenerarBajaToken()
        };
        entrada.Email = Required(email, "El email es obligatorio.").ToLowerInvariant();
        entrada.Apply(edicionPreferida, fechaPreferida, nombre, telefono, textoConsentimiento, now);
        return entrada;
    }

    /// <summary>
    /// The same person signs up again for the same course: their data and consent are refreshed and they go
    /// back to "not contacted" and subscribed. Internal notes are kept.
    /// </summary>
    public void ActualizarAlta(
        Edicion? edicionPreferida, string? fechaPreferida, string nombre, string telefono, string textoConsentimiento,
        DateTimeOffset now)
    {
        Apply(edicionPreferida, fechaPreferida, nombre, telefono, textoConsentimiento, now);
        Contactado = false;
        ContactadoEn = null;
        BajaEn = null;
    }

    /// <summary>Marks or unmarks the entry as contacted; marking it again keeps the first moment.</summary>
    public void MarcarContactado(bool contactado, DateTimeOffset now)
    {
        if (contactado && !Contactado)
        {
            ContactadoEn = now;
        }
        else if (!contactado)
        {
            ContactadoEn = null;
        }

        Contactado = contactado;
    }

    /// <summary>
    /// The person no longer wants the automatic emails about new dates of this course (F11). Unsubscribing
    /// again keeps the first moment. The client can still call them: only the automatic email stops.
    /// </summary>
    public void DarDeBaja(DateTimeOffset now) => BajaEn ??= now.ToUniversalTime();

    public void CambiarNotas(string? notas)
    {
        var normalizadas = string.IsNullOrWhiteSpace(notas) ? null : notas.Trim();
        if (normalizadas is { Length: > NotasMaxLength })
        {
            throw new DomainException($"Las notas no pueden superar {NotasMaxLength} caracteres.");
        }

        Notas = normalizadas;
    }

    private void Apply(
        Edicion? edicionPreferida, string? fechaPreferida, string nombre, string telefono, string textoConsentimiento,
        DateTimeOffset now)
    {
        if (edicionPreferida is not null && edicionPreferida.CursoId != CursoId)
        {
            throw new DomainException("La edición preferida no pertenece al curso.");
        }

        var fecha = string.IsNullOrWhiteSpace(fechaPreferida) ? null : fechaPreferida.Trim();
        if (fecha is { Length: > FechaPreferidaMaxLength })
        {
            throw new DomainException($"La fecha preferida no puede superar {FechaPreferidaMaxLength} caracteres.");
        }

        EdicionPreferida = edicionPreferida;
        EdicionPreferidaId = edicionPreferida?.Id;
        FechaPreferida = fecha;
        Nombre = Required(nombre, "El nombre es obligatorio.");
        Telefono = Required(telefono, "El teléfono es obligatorio.");
        TextoConsentimiento = Required(textoConsentimiento, "El texto de consentimiento es obligatorio.");
        ConsentimientoEn = now.ToUniversalTime();
        Busqueda = TextoBusqueda.Normalizar($"{Nombre} {Email} {Telefono}");
    }

    private static string GenerarBajaToken() =>
        Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(BajaTokenBytes));

    private static string Required(string? value, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new DomainException(message) : value.Trim();
}
