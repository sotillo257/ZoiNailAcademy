using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Emails.EnviarEmailsPendientes;

/// <summary>Sends the due emails of the outbox. Returns how many were sent.</summary>
public sealed record EnviarEmailsPendientesCommand : IRequest<Result<int>>;
