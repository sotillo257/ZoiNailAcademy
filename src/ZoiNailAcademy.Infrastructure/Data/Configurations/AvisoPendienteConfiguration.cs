using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Infrastructure.Data.Configurations;

public class AvisoPendienteConfiguration : IEntityTypeConfiguration<AvisoPendiente>
{
    public void Configure(EntityTypeBuilder<AvisoPendiente> builder)
    {
        builder.ToTable("AvisosPendientes");
        builder.HasKey(p => p.CursoId);

        // The job deletes the mark only if no other date was published meanwhile.
        builder.Property(p => p.Publicaciones).IsConcurrencyToken();

        builder.HasOne<Curso>()
            .WithMany()
            .HasForeignKey(p => p.CursoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
