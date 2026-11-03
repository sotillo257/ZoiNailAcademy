namespace ZoiNailAcademy.Application.Emails.Plantillas;

public static class PagoSinPlazaClientaEmail
{
    public static EmailContenido Crear(ReservaEmailModelo r, EmailMarca marca)
    {
        var cuerpo = new EmailCuerpo()
            .Titulo("Pago recibido sin plaza")
            .Aviso(
                "Ha llegado un pago con tarjeta de una reserva que ya había caducado y la edición no tiene plazas libres. " +
                "Habla con el comprador y, si no hay solución, reembolsa el pago desde el panel de Stripe.")
            .Codigo("Código de reserva", r.Codigo)
            .Datos(
                ("Curso", r.Curso),
                ("Edición", EmailFormato.FechaLarga(r.Inicio, marca.Zona)),
                ("Nombre", r.Nombre),
                ("Teléfono", r.Telefono),
                ("Email", r.Email),
                ("Importe", EmailFormato.Importe(r.Importe)),
                ("Pago en Stripe", r.StripePaymentIntentId))
            .Boton("Ver la edición", marca.Url($"/admin/ediciones/{r.EdicionId}"));

        return EmailLayout.Componer(marca, $"⚠ Pago recibido sin plaza · {r.Codigo}", cuerpo);
    }
}
