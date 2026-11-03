using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Reservas.AbandonarReserva;

/// <summary>The buyer came back from Checkout without paying: free the seat now instead of in 35 minutes.</summary>
public sealed record AbandonarReservaCommand(string Codigo) : IRequest<Result>;
