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
    public const int BusquedaMaxLength = NombreMaxLength + EmailMaxLength + TelefonoMaxLength + 2;

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

    /// <summary>
    /// Amount paid online (card or transfer/Bizum): copy of <see cref="Curso.PrecioReserva"/> when the booking was
    /// created.
    /// </summary>
    public decimal Importe { get; private set; }

    /// <summary>
    /// Copy of the full course price when the booking was created. <c>PrecioCurso - Importe</c> is paid in person.
    /// </summary>
    public decimal PrecioCurso { get; private set; }

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

    /// <summary>
    /// Name, email and phone normalized with <see cref="TextoBusqueda"/> (backoffice search without case or accents).
    /// Set once: those fields never change.
    /// </summary>
    public string Busqueda { get; private set; } = null!;

    private Reserva()
    {
    }

    /// <param name="edicion">Edition with its <see cref="Edicion.Curso"/> loaded (the prices are copied).</param>
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

        var reserva = new Reserva
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
            Importe = edicion.Curso.PrecioReserva,
            PrecioCurso = edicion.Curso.Precio,
            ExpiraEn = expiraEn.ToUniversalTime(),
            ConsentimientoEn = now.ToUniversalTime()
        };
        reserva.Busqueda = TextoBusqueda.Normalizar($"{reserva.Nombre} {reserva.Email} {reserva.Telefono}");
        return reserva;
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

    public const int HorasAmpliacionMaximas = 168;

    /// <summary>A manual booking needs at least this long before the edition closes (the buyer must transfer).</summary>
    public static readonly TimeSpan MargenMinimoManual = TimeSpan.FromHours(2);

    /// <summary>Expiry of a manual booking: its lifetime, clipped to the moment bookings close.</summary>
    public static DateTimeOffset CalcularCaducidadManual(DateTimeOffset now, TimeSpan duracion, DateTimeOffset cierre)
    {
        if (cierre - now < MargenMinimoManual)
        {
            throw new BusinessRuleException(
                ErrorCodes.EdicionNoReservable, "Para esta fecha solo se puede pagar con tarjeta");
        }

        var expira = now + duracion;
        return expira < cierre ? expira : cierre;
    }

    /// <param name="hayPlaza">
    /// Whether the edition has a free seat. Only checked when the booking holds no seat right now: expired, or
    /// still pending but past <see cref="ExpiraEn"/> (the job has not run yet).
    /// </param>
    /// <returns>True when the booking became paid; false when it already was (idempotent).</returns>
    public bool ConfirmarPagoManual(DateTimeOffset now, bool hayPlaza, string? nota)
    {
        if (Estado == EstadoReserva.Pagada)
        {
            return false;
        }

        if (Estado == EstadoReserva.Cancelada)
        {
            throw EstadoInvalido("Una reserva cancelada no se puede confirmar.");
        }

        if (Metodo == MetodoPago.Stripe)
        {
            throw EstadoInvalido("Los pagos con tarjeta se confirman solos");
        }

        if (!OcupaPlaza(now) && !hayPlaza)
        {
            throw new BusinessRuleException(ErrorCodes.SinPlazas, "La plaza ya se ha ocupado; habla con el comprador");
        }

        MarcarPagada(now, null);
        AnadirNota(nota);
        return true;
    }

    /// <param name="confirmarPagada">The client confirmed she will refund a paid booking outside the system.</param>
    /// <returns>True when the booking became cancelled; false when it already was (idempotent).</returns>
    public bool CancelarDesdeBackoffice(DateTimeOffset now, bool confirmarPagada, string? motivo)
    {
        switch (Estado)
        {
            case EstadoReserva.Cancelada:
                return false;
            case EstadoReserva.Pagada when !confirmarPagada:
                throw EstadoInvalido("La reserva está pagada: confirma que vas a devolver el pago para cancelarla.");
        }

        Estado = EstadoReserva.Cancelada;
        CanceladaEn = now;
        AnadirNota(motivo);
        return true;
    }

    /// <param name="cierre">Moment bookings of the edition close; the expiry never goes past it.</param>
    /// <param name="hayPlaza">Whether the edition has a free seat; only checked when the booking holds no seat right now.</param>
    public void Ampliar(DateTimeOffset now, int horas, DateTimeOffset cierre, bool hayPlaza)
    {
        if (Metodo != MetodoPago.Manual || Estado is not (EstadoReserva.Pendiente or EstadoReserva.Expirada))
        {
            throw EstadoInvalido("Solo se puede ampliar una reserva por transferencia pendiente o caducada.");
        }

        if (horas is < 1 or > HorasAmpliacionMaximas)
        {
            throw new DomainException($"Las horas deben estar entre 1 y {HorasAmpliacionMaximas}.");
        }

        if (cierre <= now)
        {
            throw new BusinessRuleException(ErrorCodes.EdicionNoReservable, "Las reservas de esta fecha ya están cerradas.");
        }

        if (!OcupaPlaza(now) && !hayPlaza)
        {
            throw new BusinessRuleException(ErrorCodes.SinPlazas, "La plaza ya se ha ocupado; habla con el comprador");
        }

        // A pending booking keeps the time it had left; a reactivated one counts from now.
        var desde = Estado == EstadoReserva.Pendiente && ExpiraEn > now ? ExpiraEn : now;
        if (Estado == EstadoReserva.Expirada)
        {
            Estado = EstadoReserva.Pendiente;
            ExpiradaEn = null;
        }

        var nueva = desde.AddHours(horas);
        ExpiraEn = nueva < cierre ? nueva : cierre;
    }

    /// <summary>
    /// The client handled a payment that arrived without a seat (usually refunding it): clears
    /// <see cref="RequiereRevision"/> and appends the note. Nothing changes when the mark is already clear.
    /// </summary>
    public void MarcarRevisada(string? nota)
    {
        if (!RequiereRevision)
        {
            return;
        }

        RequiereRevision = false;
        AnadirNota(nota);
    }

    private void AnadirNota(string? nota)
    {
        if (string.IsNullOrWhiteSpace(nota))
        {
            return;
        }

        var combinada = string.IsNullOrEmpty(NotaInterna) ? nota.Trim() : $"{NotaInterna}\n{nota.Trim()}";
        NotaInterna = combinada.Length <= NotaInternaMaxLength ? combinada : combinada[..NotaInternaMaxLength];
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
