using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.ListasEspera.DarseDeBaja;

/// <summary>
/// Sets <c>BajaEn</c> on the entry that owns the token. Always succeeds (an invalid token, or one already used,
/// answers the same), so the endpoint never reveals whether someone is on a waiting list.
/// </summary>
public class DarseDeBajaHandler(IAppDbContext dbContext, TimeProvider timeProvider, ILogger<DarseDeBajaHandler> logger)
    : IRequestHandler<DarseDeBajaCommand, Result>
{
    public async Task<Result> Handle(DarseDeBajaCommand request, CancellationToken cancellationToken)
    {
        var token = request.Token?.Trim();
        if (string.IsNullOrEmpty(token) || token.Length > ListaEspera.BajaTokenMaxLength)
        {
            return Result.Success();
        }

        var entrada = await dbContext.ListaEspera.FirstOrDefaultAsync(e => e.BajaToken == token, cancellationToken);
        if (entrada is null || entrada.BajaEn is not null)
        {
            return Result.Success();
        }

        entrada.DarDeBaja(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("La entrada {ListaEsperaId} de la lista de espera se dio de baja de los avisos.", entrada.Id);
        return Result.Success();
    }
}
