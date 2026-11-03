using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ZoiNailAcademy.Infrastructure.Data.Seeding;

/// <summary>
/// Creates the backoffice users from configuration. Existing users are never overwritten, so the
/// client can change her password; the only exception is <see cref="SuperAdminSeedOptions.ResetAdminPassword"/>.
/// </summary>
public class UserSeeder(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IOptions<AdminSeedOptions> adminOptions,
    IOptions<SuperAdminSeedOptions> superAdminOptions,
    TimeProvider timeProvider,
    ILogger<UserSeeder> logger)
{
    public const int MinimumPasswordLength = 10;

    public async Task SeedAsync(bool isProduction, CancellationToken cancellationToken = default)
    {
        var admin = Validate(adminOptions.Value, AdminSeedOptions.SectionName, isProduction);
        var superAdmin = Validate(superAdminOptions.Value, SuperAdminSeedOptions.SectionName, isProduction);

        if (admin is not null)
        {
            var existingAdmin = await EnsureUserAsync(admin.Value, UserRole.Admin, "Admin", cancellationToken);
            if (existingAdmin is not null && superAdminOptions.Value.ResetAdminPassword)
            {
                await ResetPasswordAsync(existingAdmin, admin.Value.Password, cancellationToken);
            }
        }
        else if (superAdminOptions.Value.ResetAdminPassword)
        {
            logger.LogWarning("SuperAdmin:ResetAdminPassword=true pero la configuración Admin está incompleta; no se resetea nada.");
        }

        if (superAdmin is not null)
        {
            await EnsureUserAsync(superAdmin.Value, UserRole.SuperAdmin, "SuperAdmin", cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <returns>The user when it already existed; null when it was just created.</returns>
    private async Task<User?> EnsureUserAsync(
        (string Email, string Password, string? Nombre) seed, UserRole role, string defaultName, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == seed.Email, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var name = string.IsNullOrWhiteSpace(seed.Nombre) ? defaultName : seed.Nombre.Trim();
        dbContext.Users.Add(User.Create(seed.Email, name, passwordHasher.Hash(seed.Password), role));
        logger.LogInformation("Usuario {Role} sembrado: {Email}", role, seed.Email);
        return null;
    }

    private async Task ResetPasswordAsync(User admin, string newPassword, CancellationToken cancellationToken)
    {
        admin.UpdatePasswordHash(passwordHasher.Hash(newPassword));

        var now = timeProvider.GetUtcNow();
        var activeTokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == admin.Id && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            token.Revoke(now);
        }

        logger.LogWarning(
            "Contraseña del Admin {Email} reseteada por SuperAdmin:ResetAdminPassword=true. Vuelve a ponerlo a false.",
            admin.Email);
    }

    private (string Email, string Password, string? Nombre)? Validate(SeedUserOptions options, string section, bool isProduction)
    {
        string? problem = null;
        if (string.IsNullOrWhiteSpace(options.SeedEmail))
        {
            problem = $"Falta la configuración '{section}:SeedEmail' ({section}__SeedEmail).";
        }
        else if (string.IsNullOrEmpty(options.SeedPassword))
        {
            problem = $"Falta la configuración '{section}:SeedPassword' ({section}__SeedPassword).";
        }
        else if (options.SeedPassword.Length < MinimumPasswordLength)
        {
            problem = $"'{section}:SeedPassword' debe tener al menos {MinimumPasswordLength} caracteres.";
        }

        if (problem is null && options.SeedEmail is { } email && options.SeedPassword is { } password)
        {
            return (email.Trim().ToLowerInvariant(), password, options.SeedNombre);
        }

        if (isProduction)
        {
            throw new InvalidOperationException(problem);
        }

        logger.LogWarning("No se siembra el usuario {Section}: {Problem}", section, problem);
        return null;
    }
}
