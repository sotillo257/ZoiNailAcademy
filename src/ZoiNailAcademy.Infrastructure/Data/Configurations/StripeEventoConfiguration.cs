using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Infrastructure.Data.Configurations;

public class StripeEventoConfiguration : IEntityTypeConfiguration<StripeEvento>
{
    public void Configure(EntityTypeBuilder<StripeEvento> builder)
    {
        builder.ToTable("StripeEventos");
        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventId).HasMaxLength(StripeEvento.EventIdMaxLength);
        builder.Property(e => e.Tipo).IsRequired().HasMaxLength(StripeEvento.TipoMaxLength);
    }
}
