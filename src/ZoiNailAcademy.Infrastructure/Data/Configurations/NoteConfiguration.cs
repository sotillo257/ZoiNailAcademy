using ZoiNailAcademy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZoiNailAcademy.Infrastructure.Data.Configurations;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.Property(n => n.Title).IsRequired().HasMaxLength(Note.TitleMaxLength);
        builder.Property(n => n.Content).IsRequired().HasMaxLength(Note.ContentMaxLength);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => n.UserId);
    }
}
