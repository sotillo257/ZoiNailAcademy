namespace ZoiNailAcademy.Domain.Entities;

/// <summary>
/// A person of the waiting list was already told about an edition (F11). Inserted in the same save as the email, so
/// nobody gets two notices for the same date, even if the edition is hidden and published again.
/// Primary key <c>(ListaEsperaId, EdicionId)</c>.
/// </summary>
public class AvisoEnviado
{
    public Guid ListaEsperaId { get; private set; }
    public Guid EdicionId { get; private set; }
    public DateTimeOffset EncoladoEn { get; private set; }

    private AvisoEnviado()
    {
    }

    public static AvisoEnviado Create(Guid listaEsperaId, Guid edicionId, DateTimeOffset now) => new()
    {
        ListaEsperaId = listaEsperaId,
        EdicionId = edicionId,
        EncoladoEn = now.ToUniversalTime()
    };
}
