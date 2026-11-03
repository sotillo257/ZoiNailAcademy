using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.ListasEspera.DarseDeBaja;

/// <summary>Stops the new-dates emails of one waiting list entry (one course). Unknown tokens are ignored.</summary>
public sealed record DarseDeBajaCommand(string? Token) : IRequest<Result>;
