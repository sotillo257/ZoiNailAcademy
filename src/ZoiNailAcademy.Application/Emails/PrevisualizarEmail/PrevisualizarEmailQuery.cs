using MediatR;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Emails.Plantillas;

namespace ZoiNailAcademy.Application.Emails.PrevisualizarEmail;

/// <param name="Tipo">One of <see cref="TiposEmail.Todos"/>.</param>
public sealed record PrevisualizarEmailQuery(string Tipo) : IRequest<Result<EmailContenido>>;
