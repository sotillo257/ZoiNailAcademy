namespace ZoiNailAcademy.Application.Emails.Plantillas;

/// <summary>Only for manual bookings: a card buyer simply did not pay.</summary>
public static class ReservaExpiradaCompradorEmail
{
    public static EmailContenido Crear(ReservaEmailModelo r, EmailMarca marca)
    {
        var cuerpo = new EmailCuerpo()
            .Titulo("Tu reserva ha caducado")
            .Parrafo(
                $"Hola, {r.Nombre}. No hemos recibido el pago de tu reserva en {r.Curso} " +
                $"({EmailFormato.FechaLarga(r.Inicio, marca.Zona)}) a tiempo, así que la plaza se ha liberado.")
            .Codigo("Código de reserva", r.Codigo)
            .Aviso("Si ya pagaste, contesta a este email o escríbenos por WhatsApp con el comprobante y lo revisamos.")
            .Parrafo("Si todavía quieres apuntarte, puedes reservar de nuevo mientras queden plazas.")
            .Boton("Reservar de nuevo", marca.Url($"/reservar/{r.CursoSlug}"));

        return EmailLayout.Componer(marca, $"Tu reserva ha caducado · {r.Codigo}", cuerpo);
    }
}
