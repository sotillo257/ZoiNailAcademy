namespace ZoiNailAcademy.Infrastructure.Settings;

/// <summary>Section <c>Avisos</c> (new-dates notices to the waiting list, F11).</summary>
public class AvisosOptions
{
    public const string SectionName = "Avisos";

    /// <summary>Emails per batch.</summary>
    public int LoteTamano { get; set; } = 20;

    /// <summary>Seconds between batches (Resend rate limit).</summary>
    public int PausaEntreLotesSegundos { get; set; } = 2;

    /// <summary>Minutes between runs of the notices job.</summary>
    public int JobIntervaloMinutos { get; set; } = 10;
}
