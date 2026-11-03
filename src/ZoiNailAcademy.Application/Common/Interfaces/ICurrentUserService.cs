namespace ZoiNailAcademy.Application.Common.Interfaces;

public interface ICurrentUserService
{
    /// <exception cref="Exceptions.UnauthorizedException">No hay usuario autenticado en la petición actual.</exception>
    Guid GetUserId();
}
