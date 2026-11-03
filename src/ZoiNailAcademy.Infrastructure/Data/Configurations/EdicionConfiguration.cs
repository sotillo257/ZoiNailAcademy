using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Infrastructure.Data.Configurations;

public class EdicionConfiguration : IEntityTypeConfiguration<Edicion>
{
    public void Configure(EntityTypeBuilder<Edicion> builder)
    {
        builder.ToTable("Ediciones");

        builder.Property(e => e.Estado).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Notas).HasMaxLength(Edicion.NotasMaxLength);

        // PostgreSQL xmin as optimistic concurrency token: the backoffice sends back the version it
        // read, and two simultaneous saves cannot both commit.
        builder.Property(e => e.Version).IsRowVersion();

        builder.HasOne(e => e.Curso)
            .WithMany()
            .HasForeignKey(e => e.CursoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.CursoId, e.Inicio });
    }
}
