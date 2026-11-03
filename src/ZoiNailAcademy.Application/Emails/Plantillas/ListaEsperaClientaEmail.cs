namespace ZoiNailAcademy.Application.Emails.Plantillas;

public static class ListaEsperaClientaEmail
{
    public static EmailContenido Crear(ListaEsperaEmailModelo m, EmailMarca marca)
    {
        var cuerpo = new EmailCuerpo()
            .Titulo("Nuevo interesado en la lista de espera")
            .Parrafo($"{m.Nombre} quiere que le avises cuando haya fechas de {m.Curso}.")
            .Datos(
                ("Curso", m.Curso),
                ("Nombre", m.Nombre),
                ("Teléfono", m.Telefono),
                ("Email", m.Email),
                ("Fecha preferida", m.FechaPreferida),
                ("Edición que quería", m.EdicionPreferida is { } e ? EmailFormato.FechaLarga(e, marca.Zona) : null))
            .Boton("Ver la lista de espera", marca.Url("/admin/lista-espera"));

        return EmailLayout.Componer(marca, $"Nuevo interesado · {m.Curso}", cuerpo);
    }
}
