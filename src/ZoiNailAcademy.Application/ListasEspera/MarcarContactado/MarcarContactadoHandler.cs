using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Application.ListasEspera.MarcarContactado;

public class MarcarContactadoHandler(IAppDbContext dbContext, IAppSettings appSettings, TimeProvider timeProvider)
    : IRequestHandler<MarcarContactadoCommand, Result<ListaEsperaAdminDto>>
{
    public async Task<Result<ListaEsperaAdminDto>> Handle(MarcarContactadoCommand request, CancellationToken cancellationToken)
    {
        var entrada = await dbContext.ListaEspera
            .Include(e => e.Curso)
            .Include(e => e.EdicionPreferida)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Lista de espera", request.Id);

        entrada.MarcarContactado(request.Contactado, timeProvider.GetUtcNow());
        if (request.Notas.HasValue)
        {
            entrada.CambiarNotas(request.Notas.Value);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ListaEsperaAdminDto.From(entrada, appSettings.TimeZone));
    }
}
