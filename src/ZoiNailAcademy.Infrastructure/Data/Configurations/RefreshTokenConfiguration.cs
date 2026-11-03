using ZoiNailAcademy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZoiNailAcademy.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.Ignore(t => t.IsRevoked);
        builder.Ignore(t => t.WasRotated);

        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(128);

        // PostgreSQL xmin as optimistic concurrency token: two simultaneous rotations of the
        // same refresh token cannot both commit (the second one gets DbUpdateConcurrencyException).
        builder.Property<uint>("Version").IsRowVersion();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.UserId);
    }
}
