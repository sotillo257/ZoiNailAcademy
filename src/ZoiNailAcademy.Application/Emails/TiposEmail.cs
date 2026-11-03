namespace ZoiNailAcademy.Application.Emails;

/// <summary>Template names, stored in <c>EmailSaliente.Tipo</c> and used by the preview endpoint.</summary>
public static class TiposEmail
{
    public const string ReservaPagadaComprador = "ReservaPagada.Comprador";
    public const string ReservaPagadaClienta = "ReservaPagada.Clienta";
    public const string ReservaManualComprador = "ReservaManual.Comprador";
    public const string ReservaManualClienta = "ReservaManual.Clienta";
    public const string ReservaExpiradaComprador = "ReservaExpirada.Comprador";
    public const string PagoSinPlazaClienta = "PagoSinPlaza.Clienta";
    public const string ListaEsperaClienta = "ListaEspera.Clienta";
    public const string ContactoClienta = "Contacto.Clienta";
    public const string ContactoAcuse = "Contacto.Acuse";

    public static readonly IReadOnlyList<string> Todos =
    [
        ReservaPagadaComprador, ReservaPagadaClienta, ReservaManualComprador, ReservaManualClienta,
        ReservaExpiradaComprador, PagoSinPlazaClienta, ListaEsperaClienta, ContactoClienta, ContactoAcuse
    ];
}
