using MediatR;

namespace ZoiNailAcademy.Application.Reservas;

/// <summary>A booking was paid (card or manual). F06 emails the buyer and the client.</summary>
public sealed record ReservaPagadaEvent(Guid ReservaId) : INotification;

/// <summary>A pending booking expired and its seat is free again.</summary>
public sealed record ReservaExpiradaEvent(Guid ReservaId) : INotification;

/// <summary>A payment arrived for an expired or cancelled booking and there was no seat: the client must refund it.</summary>
public sealed record PagoTrasCaducidadSinPlazaEvent(Guid ReservaId) : INotification;

/// <summary>A manual (transfer/Bizum) booking was created: F06 emails the payment details and warns the client.</summary>
public sealed record ReservaManualCreadaEvent(Guid ReservaId) : INotification;

/// <summary>A booking was cancelled from the backoffice or because its edition was cancelled.</summary>
public sealed record ReservaCanceladaEvent(Guid ReservaId) : INotification;
