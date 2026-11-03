using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Emails.LimpiarEmailsEnviados;

public class LimpiarEmailsEnviadosHandler(IAppDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<LimpiarEmailsEnviadosCommand, Result<int>>
{
    public async Task<Result<int>> Handle(LimpiarEmailsEnviadosCommand request, CancellationToken cancellationToken)
    {
        var limite = timeProvider.GetUtcNow().AddDays(-EmailSaliente.DiasRetencion);
        var borrados = await dbContext.EmailsSalientes
            .Where(e => e.Estado == EstadoEmail.Enviado && e.EnviadoEn < limite)
            .ExecuteDeleteAsync(cancellationToken);
        return Result.Success(borrados);
    }
}
