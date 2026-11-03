namespace ZoiNailAcademy.Domain.Common.Exceptions;

/// <summary>Business error codes of the API contract, written to <c>ProblemDetails.extensions.code</c>.</summary>
public static class ErrorCodes
{
    public const string SinPlazas = "sin_plazas";
    public const string EdicionNoReservable = "edicion_no_reservable";
    public const string LimitePendientes = "limite_pendientes";
    public const string PlazasPorDebajoDeOcupadas = "plazas_por_debajo_de_ocupadas";
    public const string EdicionConReservas = "edicion_con_reservas";
    public const string CambioFechaRequiereConfirmacion = "cambio_fecha_requiere_confirmacion";
    public const string EstadoInvalido = "estado_invalido";
    public const string Concurrencia = "concurrencia";
    public const string CursoConEdiciones = "curso_con_ediciones";
    public const string NoEncontrado = "no_encontrado";
}
