using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Infrastructure.Data.Configurations;

public class AvisoEnviadoConfiguration : IEntityTypeConfiguration<AvisoEnviado>
{
    public void Configure(EntityTypeBuilder<AvisoEnviado> builder)
    {
        builder.ToTable("AvisosEnviados");
        builder.HasKey(a => new { a.ListaEsperaId, a.EdicionId });

        builder.HasOne<ListaEspera>()
            .WithMany()
            .HasForeignKey(a => a.ListaEsperaId)
            .OnDelete(DeleteBehavior.Cascade);

        // The job looks up who was already told about the editions of a course.
        builder.HasOne<Edicion>()
            .WithMany()
            .HasForeignKey(a => a.EdicionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(a => a.EdicionId);
    }
}
