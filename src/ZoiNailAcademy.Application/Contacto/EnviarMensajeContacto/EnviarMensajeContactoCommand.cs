using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Contacto.EnviarMensajeContacto;

/// <summary>Body of <c>POST /api/contacto</c>. Nothing is stored except the outbox email.</summary>
/// <param name="Website">Honeypot: hidden in the form, only bots fill it.</param>
public sealed record EnviarMensajeContactoCommand(
    string Nombre, string Email, string? Telefono, string Mensaje, bool AceptaPrivacidad, string? Website)
    : IRequest<Result>;
