using System.Security.Cryptography;

namespace ZoiNailAcademy.Application.AvisosNuevasEdiciones;

/// <summary>
/// <c>EmailSaliente.ReferenciaId</c> of a new-dates notice: a hash of the waiting list entry and the editions in the
/// email, so the same notice can never be enqueued twice (unique <c>(Tipo, ReferenciaId)</c>).
/// </summary>
public static class AvisoReferencia
{
    public static Guid Calcular(Guid listaEsperaId, IEnumerable<Guid> edicionIds)
    {
        var bytes = new List<byte>(listaEsperaId.ToByteArray());
        foreach (var edicionId in edicionIds.Distinct().Order())
        {
            bytes.AddRange(edicionId.ToByteArray());
        }

        return new Guid(SHA256.HashData(bytes.ToArray()).AsSpan(0, 16));
    }
}
