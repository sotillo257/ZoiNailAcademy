namespace ZoiNailAcademy.Application.Emails.Plantillas;

public static class ReservaManualCompradorEmail
{
    public static EmailContenido Crear(ReservaEmailModelo r, DatosPagoEmail pago, EmailMarca marca)
    {
        var cuerpo = new EmailCuerpo()
            .Titulo("Completa tu pago para confirmar la plaza")
            .Parrafo(
                $"Hola, {r.Nombre}. Te guardamos la plaza en {r.Curso} ({EmailFormato.FechaLarga(r.Inicio, marca.Zona)}) " +
                $"hasta el {EmailFormato.FechaLarga(r.ExpiraEn, marca.Zona)}. Si no recibimos el pago antes, la plaza se liberará.")
            .Codigo("Concepto de la transferencia o Bizum", r.Codigo)
            .Datos(
                ("Importe", EmailFormato.Importe(r.Importe)),
                ("Titular", pago.Titular),
                ("IBAN", pago.Iban),
                ("Bizum", pago.BizumTelefono),
                ("Concepto", r.Codigo),
                ("Fecha límite", EmailFormato.FechaLarga(r.ExpiraEn, marca.Zona)))
            .Parrafo("Cuando hayas pagado, envíanos el comprobante por WhatsApp y confirmaremos tu plaza.")
            .Boton("Enviar comprobante por WhatsApp", pago.WhatsappUrl)
            .Parrafo($"Puedes volver a consultar estos datos en {marca.Url($"/reserva/{r.Codigo}")}.");

        return EmailLayout.Componer(marca, $"Completa tu pago · {r.Codigo}", cuerpo);
    }
}
