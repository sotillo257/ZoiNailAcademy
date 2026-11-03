namespace ZoiNailAcademy.Domain.Entities;

/// <summary>
/// A course with newly published editions whose waiting list has not been notified yet (F11). Publishing an edition
/// marks its course; <c>AvisosNuevasEdicionesJob</c> sends one email per person with every new date and removes the
/// mark, so several dates published close together go in a single email. Primary key <c>CursoId</c>.
/// </summary>
public class AvisoPendiente
{
    public Guid CursoId { get; private set; }

    /// <summary>First publication not notified yet (later ones keep it).</summary>
    public DateTimeOffset PendienteDesde { get; private set; }

    /// <summary>
    /// Publications since the mark was created. Concurrency token: if a date is published while the job is notifying
    /// the course, the job's delete of the mark fails and the next cycle includes the new date.
    /// </summary>
    public int Publicaciones { get; private set; }

    private AvisoPendiente()
    {
    }

    public static AvisoPendiente Create(Guid cursoId, DateTimeOffset now) => new()
    {
        CursoId = cursoId,
        PendienteDesde = now.ToUniversalTime(),
        Publicaciones = 1
    };

    /// <summary>Another edition of the course was published before the job ran.</summary>
    public void RegistrarPublicacion() => Publicaciones++;
}
