using Microsoft.Extensions.Options;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Infrastructure.Email;

public class EmailSettings(IOptions<EmailOptions> options) : IEmailSettings
{
    public string? ReplyTo => Optional(options.Value.ReplyTo);
    public string? AvisosClienta => Optional(options.Value.AvisosClienta);
    public bool AcuseContacto => options.Value.AcuseContacto;

    private static string? Optional(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
