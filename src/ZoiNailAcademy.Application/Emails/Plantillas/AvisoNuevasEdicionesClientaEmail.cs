namespace ZoiNailAcademy.Application.Emails.Plantillas;

/// <summary>Summary for the client after the waiting list of a course was told about new dates (F11).</summary>
public static class AvisoNuevasEdicionesClientaEmail
{
    public static EmailContenido Crear(AvisoNuevasEdicionesClientaModelo m, EmailMarca marca)
    {
        var interesados = m.Interesados == 1 ? "1 interesado" : $"{m.Interesados} interesados";
        var cuerpo = new EmailCuerpo()
            .Titulo("Aviso de nuevas fechas enviado")
            .Parrafo($"Hemos avisado por email a {interesados} de la lista de espera de {m.Curso} de estas fechas:")
            .Datos(m.Fechas
                .OrderBy(f => f.Inicio)
                .Select(f => (EmailFormato.FechaLarga(f.Inicio, marca.Zona), (string?)$"{f.PlazasLibres} libres"))
                .ToArray())
            .Parrafo("Cada persona recibe un solo email con las fechas que aún no se le habían avisado.")
            .Boton("Ver la lista de espera", marca.Url("/admin/lista-espera"));

        return EmailLayout.Componer(marca, $"Aviso enviado a {interesados} · {m.Curso}", cuerpo);
    }
}
