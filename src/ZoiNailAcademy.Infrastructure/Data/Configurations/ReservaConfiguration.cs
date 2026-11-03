using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Services;

namespace ZoiNailAcademy.Infrastructure.Data.Configurations;

public class ReservaConfiguration : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> builder)
    {
        builder.ToTable("Reservas");

        builder.Property(r => r.Codigo).IsRequired().HasMaxLength(CodigoReserva.Longitud);
        builder.Property(r => r.Metodo).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Estado).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Nombre).IsRequired().HasMaxLength(Reserva.NombreMaxLength);
        builder.Property(r => r.Email).IsRequired().HasMaxLength(Reserva.EmailMaxLength);
        builder.Property(r => r.Telefono).IsRequired().HasMaxLength(Reserva.TelefonoMaxLength);
        builder.Property(r => r.Importe).HasPrecision(10, 2);
        builder.Property(r => r.PrecioCurso).HasPrecision(10, 2);
        builder.Property(r => r.StripeSessionId).HasMaxLength(Reserva.StripeIdMaxLength);
        builder.Property(r => r.StripePaymentIntentId).HasMaxLength(Reserva.StripeIdMaxLength);
        builder.Property(r => r.NotaInterna).HasMaxLength(Reserva.NotaInternaMaxLength);
        builder.Property(r => r.Busqueda).IsRequired().HasMaxLength(Reserva.BusquedaMaxLength);

        // PostgreSQL xmin: the webhook, the F04 job and the backoffice may change a booking at the same time.
        builder.Property(r => r.Version).IsRowVersion();

        builder.HasOne(r => r.Edicion)
            .WithMany()
            .HasForeignKey(r => r.EdicionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.Codigo).IsUnique();
        builder.HasIndex(r => r.StripeSessionId).IsUnique().HasFilter(@"""StripeSessionId"" IS NOT NULL");
        builder.HasIndex(r => new { r.EdicionId, r.Estado });
        builder.HasIndex(r => new { r.Email, r.Estado });
        builder.HasIndex(r => new { r.Telefono, r.Estado });
        builder.HasIndex(r => new { r.Estado, r.ExpiraEn });
    }
}
