using System.Linq.Expressions;
using ZoiNailAcademy.Domain.Common;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Domain.Services;

namespace ZoiNailAcademy.Domain.Entities;

/// <summary>A booking of one seat in an edition, paid by card (Stripe) or manually (F04).</summary>
public class Reserva : BaseAuditableEntity
{
    public const int NombreMaxLength = 120;
    public const int EmailMaxLength = 200;
    public const int TelefonoMaxLength = 20;
    public const int StripeIdMaxLength = 100;
    public const int NotaInternaMaxLength = 500;

    public string Codigo { get; private set; } = null!;
    public Guid EdicionId { get; private set; }
    public Edicion Edicion { get; private set; } = null!;
    public MetodoPago Metodo { get; private set; }
    public EstadoReserva Estado { get; private set; }
    public string Nombre { get; private set; } = null!;

    /// <summary>Trimmed and lower-cased.</summary>
    public string Email { get; private set; } = null!;

    /// <summary>E.164, e.g. <c>+34612345678</c>.</summary>
    public string Telefono { get; private set; } = null!;

    /// <summary>Copy of the course price when the booking was created.</summary>
    public decimal Importe { get; private set; }

    public string? StripeSessionId { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public DateTimeOffset ExpiraEn { get; private set; }
    public DateTimeOffset? PagadaEn { get; private set; }
    public DateTimeOffset? CanceladaEn { get; private set; }
    public DateTimeOffset? ExpiradaEn { get; private set; }

    /// <summary>A payment arrived when the booking had already expired and there was no seat left.</summary>
    public bool RequiereRevision { get; private set; }

    public string? NotaInterna { get; private set; }

    /// <summary>When the buyer accepted the terms and the privacy policy.</summary>
    public DateTimeOffset ConsentimientoEn { get; private set; }

    /// <summary>Optimistic concurrency token (PostgreSQL <c>xmin</c>).</summary>
    public uint Version { get; private set; }

    private Reserva()
    {
    }

    /// <param name="edicion">Edition with its <see cref="Edicion.Curso"/> loaded (the price is copied).</param>
    public static Reserva Create(
        Edicion edicion, string codigo, MetodoPago metodo, string nombre, string email, string telefono,
        DateTimeOffset expiraEn, DateTimeOffset now)
    {
        if (!CodigoReserva.EsValido(codigo))
        {
            throw new DomainException("El código de reserva no es válido.");
        }

        if (!Enum.IsDefined(metodo))
        {
            throw new DomainException("El método de pago no es válido.");
        }

        if (expiraEn <= now)
        {
            throw new DomainException("La caducidad de la reserva debe estar en el futuro.");
        }

        return new Reserva
        {
            Id = Guid.NewGuid(),
            Codigo = codigo,
            Edicion = edicion,
            EdicionId = edicion.Id,
            Metodo = metodo,
            Estado = EstadoReserva.Pendiente,
            Nombre = Required(nombre, "El nombre es obligatorio."),
            Email = Required(email, "El email es obligatorio.").ToLowerInvariant(),
            Telefono = Required(telefono, "El teléfono es obligatorio."),
            Importe = edicion.Curso.Precio,
            ExpiraEn = expiraEn.ToUniversalTime(),
            ConsentimientoEn = now.ToUniversalTime()
        };
    }

    /// <summary>Same rule as <see cref="PlazasCalculator.OcupaPlaza"/>, translatable to SQL.</summary>
    public static Expression<Func<Reserva, bool>> OcupaPlazaEn(DateTimeOffset now) =>
        r => r.Estado == EstadoReserva.Pagada || (r.Estado == EstadoReserva.Pendiente && r.ExpiraEn > now);

    public bool OcupaPlaza(DateTimeOffset now) => PlazasCalculator.OcupaPlaza(Estado, ExpiraEn, now);

    public void AsignarSesionStripe(string sessionId)
    {
        if (Metodo != MetodoPago.Stripe || Estado != EstadoReserva.Pendiente)
        {
            throw EstadoInvalido("Solo una reserva pendiente con tarjeta puede tener sesión de pago.");
        }

        StripeSessionId = sessionId;
    }

    public void Pagar(DateTimeOffset now, string? stripePaymentIntentId = null)
    {
        if (Estado != EstadoReserva.Pendiente)
        {
            throw EstadoInvalido($"Una reserva {Estado} no se puede marcar como pagada.");
        }

        MarcarPagada(now, stripePaymentIntentId);
    }

    /// <summary>A payment arrived after the booking expired or was cancelled, and a seat is still free.</summary>
    public void PagarTrasCaducidad(DateTimeOffset now, string? stripePaymentIntentId = null)
    {
        EnsureCaducadaOCancelada();
        RequiereRevision = false;
        MarcarPagada(now, stripePaymentIntentId);
    }

    /// <summary>A payment arrived after the booking expired or was cancelled and there is no seat: the client refunds it.</summary>
    public void RegistrarPagoSinPlaza(string? stripePaymentIntentId = null)
    {
        EnsureCaducadaOCancelada();
        RequiereRevision = true;
        StripePaymentIntentId = stripePaymentIntentId ?? StripePaymentIntentId;
    }

    public void Expirar(DateTimeOffset now)
    {
        if (Estado != EstadoReserva.Pendiente)
        {
            throw EstadoInvalido($"Una reserva {Estado} no puede expirar.");
        }

        Estado = EstadoReserva.Expirada;
        ExpiradaEn = now;
    }

    public void Cancelar(DateTimeOffset now)
    {
        if (Estado != EstadoReserva.Pendiente)
        {
            throw EstadoInvalido($"Una reserva {Estado} no se puede cancelar.");
        }

        Estado = EstadoReserva.Cancelada;
        CanceladaEn = now;
    }

    private void MarcarPagada(DateTimeOffset now, string? stripePaymentIntentId)
    {
        Estado = EstadoReserva.Pagada;
        PagadaEn = now;
        StripePaymentIntentId = stripePaymentIntentId ?? StripePaymentIntentId;
    }

    private void EnsureCaducadaOCancelada()
    {
        if (Estado is not (EstadoReserva.Expirada or EstadoReserva.Cancelada))
        {
            throw EstadoInvalido($"Una reserva {Estado} no admite un pago tardío.");
        }
    }

    private static BusinessRuleException EstadoInvalido(string message) => new(ErrorCodes.EstadoInvalido, message);

    private static string Required(string? value, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new DomainException(message) : value.Trim();
}
