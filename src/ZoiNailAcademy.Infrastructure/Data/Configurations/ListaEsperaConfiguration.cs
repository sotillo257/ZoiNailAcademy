using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Infrastructure.Data.Configurations;

public class ListaEsperaConfiguration : IEntityTypeConfiguration<ListaEspera>
{
    public void Configure(EntityTypeBuilder<ListaEspera> builder)
    {
        builder.ToTable("ListaEspera");

        builder.Property(e => e.FechaPreferida).HasMaxLength(ListaEspera.FechaPreferidaMaxLength);
        builder.Property(e => e.Nombre).IsRequired().HasMaxLength(ListaEspera.NombreMaxLength);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(ListaEspera.EmailMaxLength);
        builder.Property(e => e.Telefono).IsRequired().HasMaxLength(ListaEspera.TelefonoMaxLength);
        builder.Property(e => e.TextoConsentimiento).IsRequired().HasMaxLength(ListaEspera.TextoConsentimientoMaxLength);
        builder.Property(e => e.Notas).HasMaxLength(ListaEspera.NotasMaxLength);
        builder.Property(e => e.BajaToken).IsRequired().HasMaxLength(ListaEspera.BajaTokenMaxLength);
        builder.Property(e => e.Busqueda).IsRequired().HasMaxLength(ListaEspera.BusquedaMaxLength);

        builder.HasOne(e => e.Curso)
            .WithMany()
            .HasForeignKey(e => e.CursoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Deleting an edition (only possible without bookings) must not be blocked by people who preferred it.
        builder.HasOne(e => e.EdicionPreferida)
            .WithMany()
            .HasForeignKey(e => e.EdicionPreferidaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.CursoId, e.Email }).IsUnique();
        builder.HasIndex(e => e.BajaToken).IsUnique();
        builder.HasIndex(e => new { e.Contactado, e.CreatedAt });
    }
}
