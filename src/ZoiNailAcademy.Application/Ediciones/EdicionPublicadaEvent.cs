using MediatR;

namespace ZoiNailAcademy.Application.Ediciones;

/// <summary>
/// An edition became visible: created as <c>Programada</c> or moved from <c>Oculta</c> to
/// <c>Programada</c>. Published after the commit; F11 notifies the waiting list.
/// </summary>
public sealed record EdicionPublicadaEvent(Guid EdicionId) : INotification;
