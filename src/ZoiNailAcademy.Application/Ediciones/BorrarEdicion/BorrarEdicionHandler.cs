using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Common.Exceptions;

namespace ZoiNailAcademy.Application.Ediciones.BorrarEdicion;

public class BorrarEdicionHandler(IAppDbContext dbContext, IOcupacionReader ocupacionReader)
    : IRequestHandler<BorrarEdicionCommand, Result>
{
    public async Task<Result> Handle(BorrarEdicionCommand request, CancellationToken cancellationToken)
    {
        var edicion = await dbContext.Ediciones.FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Edición", request.Id);

        if (await ocupacionReader.TieneReservasAsync(edicion.Id, cancellationToken))
        {
            throw new ConflictException(
                ErrorCodes.EdicionConReservas,
                "La edición tiene reservas y no se puede borrar: cancélala en su lugar.");
        }

        dbContext.Ediciones.Remove(edicion);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
