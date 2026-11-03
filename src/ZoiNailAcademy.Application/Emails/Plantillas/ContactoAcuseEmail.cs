namespace ZoiNailAcademy.Application.Emails.Plantillas;

/// <summary>
/// Optional acknowledgement to the sender (<c>Email:AcuseContacto</c>). It never repeats the message, so the
/// form cannot be used to relay spam to arbitrary addresses.
/// </summary>
public static class ContactoAcuseEmail
{
    public static EmailContenido Crear(ContactoEmailModelo m, EmailMarca marca)
    {
        var cuerpo = new EmailCuerpo()
            .Titulo("Hemos recibido tu mensaje")
            .Parrafo($"Hola, {m.Nombre}. Gracias por escribirnos: te responderemos lo antes posible.")
            .Parrafo($"Un saludo,\n{marca.NombreAcademia}");

        return EmailLayout.Componer(marca, "Hemos recibido tu mensaje", cuerpo);
    }
}
