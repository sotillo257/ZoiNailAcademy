using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Curso> Cursos { get; }
    DbSet<Edicion> Ediciones { get; }
    DbSet<Reserva> Reservas { get; }
    DbSet<StripeEvento> StripeEventos { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks the edition row (<c>SELECT … FOR UPDATE</c>) until the current transaction ends, so bookings
    /// of the same edition are serialized. Must be called inside <see cref="BeginTransactionAsync"/>.
    /// </summary>
    Task BloquearEdicionAsync(Guid edicionId, CancellationToken cancellationToken = default);
}
