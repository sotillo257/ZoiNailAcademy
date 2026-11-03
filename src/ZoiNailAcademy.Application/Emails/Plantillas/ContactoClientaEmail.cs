namespace ZoiNailAcademy.Application.Emails.Plantillas;

/// <summary>Sent with Reply-To = the sender, so the client answers with "Reply".</summary>
public static class ContactoClientaEmail
{
    public static EmailContenido Crear(ContactoEmailModelo m, EmailMarca marca)
    {
        var cuerpo = new EmailCuerpo()
            .Titulo("Mensaje desde la web")
            .Datos(("Nombre", m.Nombre), ("Email", m.Email), ("Teléfono", m.Telefono))
            .Parrafo(m.Mensaje)
            .Parrafo("Responde a este email para contestar directamente.");

        return EmailLayout.Componer(marca, $"Mensaje de la web · {m.Nombre}", cuerpo);
    }
}
