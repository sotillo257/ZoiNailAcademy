namespace ZoiNailAcademy.Application.Emails.Plantillas;

public static class ReservaPagadaCompradorEmail
{
    public static EmailContenido Crear(ReservaEmailModelo r, EmailMarca marca)
    {
        var cuerpo = new EmailCuerpo()
            .Titulo("¡Tu plaza está confirmada!")
            .Parrafo($"Hola, {r.Nombre}. Hemos recibido tu pago y tu plaza en {r.Curso} está reservada.")
            .Codigo("Código de reserva", r.Codigo)
            .Datos(
                ("Curso", r.Curso),
                ("Fecha y hora", EmailFormato.FechaLarga(r.Inicio, marca.Zona)),
                ("Lugar", r.Ubicacion),
                (r.ImporteEtiqueta, EmailFormato.Importe(r.Importe)),
                ("Resto a pagar en persona", r.RestoTexto),
                ("Pago", r.MetodoTexto))
            .Parrafo(Contacto(marca))
            .Parrafo($"¡Nos vemos pronto!\n{marca.NombreAcademia}");

        return EmailLayout.Componer(marca, $"Reserva confirmada · {r.Curso} · {r.Codigo}", cuerpo);
    }

    internal static string Contacto(EmailMarca marca)
    {
        var canales = new List<string> { "contesta a este email" };
        if (!string.IsNullOrWhiteSpace(marca.WhatsAppTelefono))
        {
            canales.Add($"escríbenos por WhatsApp al +{marca.WhatsAppTelefono}");
        }

        return $"Si tienes cualquier duda, {string.Join(" o ", canales)} indicando tu código de reserva.";
    }
}
