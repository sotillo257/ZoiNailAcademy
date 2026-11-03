using ZoiNailAcademy.Domain.Common;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Domain.Entities;

/// <summary>A dated edition of a course with a limited number of seats, managed by the client (Admin).</summary>
public class Edicion : BaseAuditableEntity
{
    public const int PlazasMinimas = 1;
    public const int PlazasMaximas = 200;
    public const int NotasMaxLength = 500;

    public Guid CursoId { get; private set; }
    public Curso Curso { get; private set; } = null!;
    public DateTimeOffset Inicio { get; private set; }
    public int Plazas { get; private set; }
    public EstadoEdicion Estado { get; private set; }

    /// <summary>Internal notes, only shown in the backoffice.</summary>
    public string? Notas { get; private set; }

    /// <summary>Optimistic concurrency token (PostgreSQL <c>xmin</c>), set by the database.</summary>
    public uint Version { get; private set; }

    private Edicion()
    {
    }

    public static Edicion Create(
        Curso curso, DateTimeOffset inicio, int plazas, EstadoEdicion estado, string? notas, DateTimeOffset now)
    {
        if (!curso.Activo)
        {
            throw new DomainException("No se pueden crear ediciones de un curso inactivo.");
        }

        if (estado is not (EstadoEdicion.Programada or EstadoEdicion.Oculta))
        {
            throw new DomainException("Una edición nueva solo puede estar Programada u Oculta.");
        }

        EnsureInicioFuturo(inicio, now);
        EnsurePlazasEnRango(plazas);

        return new Edicion
        {
            Id = Guid.NewGuid(),
            Curso = curso,
            CursoId = curso.Id,
            Inicio = inicio.ToUniversalTime(),
            Plazas = plazas,
            Estado = estado,
            Notas = NormalizeNotas(notas)
        };
    }

    public bool HaEmpezado(DateTimeOffset now) => Inicio <= now;

    /// <summary>
    /// Bookable: scheduled, course active and starting after the booking closing window. An edition is
    /// visible on the web exactly when it is bookable (even when it has no free seats).
    /// </summary>
    public bool EsReservable(bool cursoActivo, DateTimeOffset now, TimeSpan cierreAntesDeInicio) =>
        Estado == EstadoEdicion.Programada && cursoActivo && Inicio - now > cierreAntesDeInicio;

    /// <param name="ocupadas">Seats held by paid or valid pending bookings.</param>
    /// <param name="confirmarCambioFecha">The client confirmed she will warn the enrolled students.</param>
    public CambioEdicion Actualizar(
        DateTimeOffset inicio, int plazas, EstadoEdicion estado, string? notas, int ocupadas,
        bool confirmarCambioFecha, DateTimeOffset now)
    {
        inicio = inicio.ToUniversalTime();
        var cambiaInicio = inicio != Inicio;
        var cambiaPlazas = plazas != Plazas;
        var cambiaEstado = estado != Estado;

        if (Estado == EstadoEdicion.Cancelada && cambiaEstado)
        {
            throw new BusinessRuleException(
                ErrorCodes.EstadoInvalido, "Una edición cancelada no se puede reactivar: crea una nueva.");
        }

        if ((HaEmpezado(now) || Estado == EstadoEdicion.Cancelada) && (cambiaInicio || cambiaPlazas || cambiaEstado))
        {
            throw new BusinessRuleException(
                ErrorCodes.EstadoInvalido,
                HaEmpezado(now)
                    ? "La edición ya ha empezado: solo se pueden editar las notas."
                    : "La edición está cancelada: solo se pueden editar las notas.");
        }

        if (!Enum.IsDefined(estado))
        {
            throw new DomainException("El estado de la edición no es válido.");
        }

        EnsurePlazasEnRango(plazas);
        if (cambiaInicio)
        {
            EnsureInicioFuturo(inicio, now);
        }

        if (plazas < ocupadas)
        {
            throw new BusinessRuleException(
                ErrorCodes.PlazasPorDebajoDeOcupadas,
                $"No puedes dejar menos plazas que las ocupadas ({ocupadas}).",
                new Dictionary<string, object?> { ["ocupadas"] = ocupadas });
        }

        if (cambiaInicio && ocupadas > 0 && !confirmarCambioFecha)
        {
            throw new BusinessRuleException(
                ErrorCodes.CambioFechaRequiereConfirmacion,
                $"Esta edición tiene {ocupadas} inscritos, tendrás que avisarles.",
                new Dictionary<string, object?> { ["reservas"] = ocupadas });
        }

        var publicada = Estado == EstadoEdicion.Oculta && estado == EstadoEdicion.Programada;
        var avisos = new List<string>();
        if (cambiaEstado && estado == EstadoEdicion.Cancelada && ocupadas > 0)
        {
            avisos.Add($"Tiene {ocupadas} inscritos: avisa y reembolsa manualmente");
        }

        Inicio = inicio;
        Plazas = plazas;
        Estado = estado;
        Notas = NormalizeNotas(notas);

        return new CambioEdicion(publicada, cambiaEstado && estado == EstadoEdicion.Cancelada, avisos);
    }

    private static void EnsureInicioFuturo(DateTimeOffset inicio, DateTimeOffset now)
    {
        if (inicio <= now)
        {
            throw new DomainException("La fecha de inicio debe estar en el futuro.");
        }
    }

    private static void EnsurePlazasEnRango(int plazas)
    {
        if (plazas is < PlazasMinimas or > PlazasMaximas)
        {
            throw new DomainException($"Las plazas deben estar entre {PlazasMinimas} y {PlazasMaximas}.");
        }
    }

    private static string? NormalizeNotas(string? notas) => string.IsNullOrWhiteSpace(notas) ? null : notas.Trim();
}

/// <param name="Publicada">The edition moved from <c>Oculta</c> to <c>Programada</c> (publish <c>EdicionPublicadaEvent</c>).</param>
/// <param name="Cancelada">The edition was just cancelled.</param>
/// <param name="Avisos">Warnings for the client.</param>
public sealed record CambioEdicion(bool Publicada, bool Cancelada, IReadOnlyList<string> Avisos);
