using MediatR;

namespace ZoiNailAcademy.Application.ListasEspera;

/// <summary>Someone joined (or re-joined) the waiting list of a course. F06 emails the client.</summary>
public sealed record ListaEsperaAltaEvent(Guid ListaEsperaId) : INotification;
