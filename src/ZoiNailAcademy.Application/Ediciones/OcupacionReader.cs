using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Application.Ediciones;

/// <summary>
/// F02 has no bookings yet, so every seat is free. F03 replaces these bodies with one query over
/// <c>Reservas</c>, grouped by <c>EdicionId</c> and filtered with the rule of
/// <c>PlazasCalculator.OcupaPlaza</c> (Pagada, or Pendiente with ExpiraEn &gt; now).
/// </summary>
public class OcupacionReader : IOcupacionReader
{
    private static readonly IReadOnlyDictionary<Guid, Ocupacion> SinReservas = new Dictionary<Guid, Ocupacion>();

    public Task<IReadOnlyDictionary<Guid, Ocupacion>> LeerAsync(
        IReadOnlyCollection<Guid> edicionIds, CancellationToken cancellationToken) =>
        // TODO F03: GroupBy over IAppDbContext.Reservas (Pagada / Pendiente vigente) with TimeProvider.
        Task.FromResult(SinReservas);

    public Task<bool> TieneReservasAsync(Guid edicionId, CancellationToken cancellationToken) =>
        // TODO F03: Reservas.AnyAsync(r => r.EdicionId == edicionId).
        Task.FromResult(false);
}
