namespace ZoiNailAcademy.Application.Emails.Plantillas;

public static class ReservaPagadaClientaEmail
{
    public static EmailContenido Crear(ReservaEmailModelo r, int plazasLibres, EmailMarca marca)
    {
        var cuerpo = new EmailCuerpo()
            .Titulo("Nueva inscripción")
            .Parrafo($"{r.Nombre} se ha inscrito en {r.Curso}.")
            .Codigo("Código de reserva", r.Codigo)
            .Datos(
                ("Edición", EmailFormato.FechaLarga(r.Inicio, marca.Zona)),
                ("Nombre", r.Nombre),
                ("Teléfono", r.Telefono),
                ("Email", r.Email),
                ("Pago", r.MetodoTexto),
                ("Importe", EmailFormato.Importe(r.Importe)),
                ("Plazas libres", plazasLibres.ToString(EmailFormato.Es)))
            .Boton("Ver la edición", marca.Url($"/admin/ediciones/{r.EdicionId}"));

        return EmailLayout.Componer(
            marca, $"Nueva inscripción · {r.Curso} {EmailFormato.FechaCorta(r.Inicio, marca.Zona)} · {r.Codigo}", cuerpo);
    }
}
