using ZoiNailAcademy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ZoiNailAcademy.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Curso> Cursos { get; }
    DbSet<Edicion> Ediciones { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
