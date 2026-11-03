namespace ZoiNailAcademy.Application.Common.Interfaces;

/// <summary>Settings of the new-dates notices to the waiting list (<c>Avisos:*</c>, F11), implemented in Infrastructure.</summary>
public interface IAvisosSettings
{
    /// <summary>Emails per batch (<c>Avisos:LoteTamano</c>): each batch is due <see cref="PausaEntreLotes"/> after the previous one.</summary>
    int LoteTamano { get; }

    /// <summary>Pause between batches (<c>Avisos:PausaEntreLotesSegundos</c>), so the provider's rate limit is not hit.</summary>
    TimeSpan PausaEntreLotes { get; }

    /// <summary>Interval of the notices job (<c>Avisos:JobIntervaloMinutos</c>).</summary>
    TimeSpan JobIntervalo { get; }
}
