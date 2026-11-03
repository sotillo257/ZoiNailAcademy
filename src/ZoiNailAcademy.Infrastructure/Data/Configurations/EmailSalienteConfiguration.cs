using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Infrastructure.Data.Configurations;

public class EmailSalienteConfiguration : IEntityTypeConfiguration<EmailSaliente>
{
    public void Configure(EntityTypeBuilder<EmailSaliente> builder)
    {
        builder.ToTable("EmailsSalientes");

        builder.Property(e => e.Para).IsRequired().HasMaxLength(EmailSaliente.EmailMaxLength);
        builder.Property(e => e.ReplyTo).HasMaxLength(EmailSaliente.EmailMaxLength);
        builder.Property(e => e.Asunto).IsRequired().HasMaxLength(EmailSaliente.AsuntoMaxLength);
        builder.Property(e => e.Html).IsRequired().HasColumnType("text");
        builder.Property(e => e.Texto).IsRequired().HasColumnType("text");
        builder.Property(e => e.Tipo).IsRequired().HasMaxLength(EmailSaliente.TipoMaxLength);
        builder.Property(e => e.Estado).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.UltimoError).HasMaxLength(EmailSaliente.UltimoErrorMaxLength);
        builder.Property(e => e.ProveedorId).HasMaxLength(EmailSaliente.ProveedorIdMaxLength);

        // An event published twice enqueues its email once.
        builder.HasIndex(e => new { e.Tipo, e.ReferenciaId }).IsUnique().HasFilter(@"""ReferenciaId"" IS NOT NULL");
        builder.HasIndex(e => new { e.Estado, e.ProximoIntentoEn });
    }
}
