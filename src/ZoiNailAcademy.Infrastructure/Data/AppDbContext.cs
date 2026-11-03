using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Common;
using ZoiNailAcademy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ZoiNailAcademy.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly TimeProvider _timeProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, TimeProvider timeProvider) : base(options)
    {
        _timeProvider = timeProvider;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Edicion> Ediciones => Set<Edicion>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<StripeEvento> StripeEventos => Set<StripeEvento>();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Database.BeginTransactionAsync(cancellationToken);

    public async Task BloquearEdicionAsync(Guid edicionId, CancellationToken cancellationToken = default)
    {
        if (Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("BloquearEdicionAsync requiere una transacción abierta.");
        }

        // Row lock held until the transaction ends: concurrent bookings of this edition wait here.
        await Database.ExecuteSqlInterpolatedAsync(
            $@"SELECT 1 FROM ""Ediciones"" WHERE ""Id"" = {edicionId} FOR UPDATE", cancellationToken);
    }

    public async Task<IAsyncDisposable?> TryAdquirirLockAsync(long clave, CancellationToken cancellationToken = default)
    {
        // Session-level lock: it lives as long as this connection, so the connection stays open until disposal.
        await Database.OpenConnectionAsync(cancellationToken);
        var adquirido = (await Database
            .SqlQuery<bool>($"SELECT pg_try_advisory_lock({clave}) AS \"Value\"")
            .ToListAsync(cancellationToken)).Single();

        if (adquirido)
        {
            return new AdvisoryLock(this, clave);
        }

        await Database.CloseConnectionAsync();
        return null;
    }

    public void DescartarCambios() => ChangeTracker.Clear();

    private sealed class AdvisoryLock(AppDbContext dbContext, long clave) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_unlock({clave})");
            }
            finally
            {
                // Closing the connection also releases the lock if the unlock above failed.
                await dbContext.Database.CloseConnectionAsync();
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
