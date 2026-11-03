using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZoiNailAcademy.Application.Contacto.EnviarMensajeContacto;
using ZoiNailAcademy.Host.RateLimiting;

namespace ZoiNailAcademy.Host.Controllers;

/// <summary>Public contact form: the message is emailed to the client (nothing else is stored).</summary>
[ApiController]
[Route("contacto")]
[AllowAnonymous]
public class ContactoController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Formularios)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Enviar(EnviarMensajeContactoCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return Accepted();
    }
}
