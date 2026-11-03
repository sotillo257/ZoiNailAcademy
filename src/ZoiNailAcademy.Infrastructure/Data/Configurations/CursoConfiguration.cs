using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Infrastructure.Data.Configurations;

public class CursoConfiguration : IEntityTypeConfiguration<Curso>
{
    public void Configure(EntityTypeBuilder<Curso> builder)
    {
        builder.ToTable("Cursos");

        builder.Property(c => c.Nombre).IsRequired().HasMaxLength(Curso.NombreMaxLength);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(Curso.SlugMaxLength);
        builder.Property(c => c.DescripcionCorta).IsRequired().HasMaxLength(Curso.DescripcionCortaMaxLength);
        builder.Property(c => c.Descripcion).IsRequired().HasColumnType("text");
        builder.Property(c => c.Precio).HasPrecision(10, 2);
        builder.Property(c => c.Imagen).IsRequired().HasMaxLength(Curso.ImagenMaxLength);
        builder.Property(c => c.Ubicacion).HasMaxLength(Curso.UbicacionMaxLength);
        builder.Property(c => c.DuracionTexto).HasMaxLength(Curso.DuracionTextoMaxLength);

        builder.HasIndex(c => c.Slug).IsUnique();
    }
}
