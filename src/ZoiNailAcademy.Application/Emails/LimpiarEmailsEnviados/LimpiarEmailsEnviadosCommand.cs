using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Emails.LimpiarEmailsEnviados;

/// <summary>Deletes sent emails older than <c>EmailSaliente.DiasRetencion</c> days (GDPR). Returns how many.</summary>
public sealed record LimpiarEmailsEnviadosCommand : IRequest<Result<int>>;
