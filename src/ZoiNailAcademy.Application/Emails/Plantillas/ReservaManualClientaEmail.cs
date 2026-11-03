namespace ZoiNailAcademy.Application.Emails.Plantillas;

public static class ReservaManualClientaEmail
{
    public static EmailContenido Crear(ReservaEmailModelo r, EmailMarca marca)
    {
        var cuerpo = new EmailCuerpo()
            .Titulo("Reserva pendiente de pago")
            .Parrafo($"{r.Nombre} ha reservado plaza en {r.Curso} y pagará por transferencia o Bizum.")
            .Codigo("Código de reserva", r.Codigo)
            .Datos(
                ("Edición", EmailFormato.FechaLarga(r.Inicio, marca.Zona)),
                ("Nombre", r.Nombre),
                ("Teléfono", r.Telefono),
                ("Email", r.Email),
                ("Importe", EmailFormato.Importe(r.Importe)),
                ("Caduca", EmailFormato.FechaLarga(r.ExpiraEn, marca.Zona)))
            .Parrafo("Cuando te llegue el comprobante, confirma el pago desde el backoffice.")
            .Boton("Ver pendientes de pago", marca.Url("/admin/reservas"));

        return EmailLayout.Componer(marca, $"Reserva pendiente de pago · {r.Codigo}", cuerpo);
    }
}
