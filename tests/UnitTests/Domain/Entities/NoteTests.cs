using FluentAssertions;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;

namespace UnitTests.Domain.Entities;

public class NoteTests
{
    [Fact]
    public void Create_WithValidData_SetsPropertiesAndNewId()
    {
        var userId = Guid.NewGuid();

        var note = Note.Create(userId, "Lista de la compra", "Leche, pan");

        note.Id.Should().NotBeEmpty();
        note.UserId.Should().Be(userId);
        note.Title.Should().Be("Lista de la compra");
        note.Content.Should().Be("Leche, pan");
    }

    [Fact]
    public void Create_WithSurroundingWhitespaceInTitle_TrimsTitle()
    {
        var note = Note.Create(Guid.NewGuid(), "  Ideas  ", "");

        note.Title.Should().Be("Ideas");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankTitle_ThrowsDomainException(string title)
    {
        var act = () => Note.Create(Guid.NewGuid(), title, "contenido");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_WithValidData_ReplacesTitleAndContent()
    {
        var note = Note.Create(Guid.NewGuid(), "Original", "Texto original");

        note.Update("Editada", "Texto editado");

        note.Title.Should().Be("Editada");
        note.Content.Should().Be("Texto editado");
    }

    [Fact]
    public void Update_WithBlankTitle_ThrowsDomainExceptionAndKeepsState()
    {
        var note = Note.Create(Guid.NewGuid(), "Original", "Texto original");

        var act = () => note.Update(" ", "Otro texto");

        act.Should().Throw<DomainException>();
        note.Title.Should().Be("Original");
        note.Content.Should().Be("Texto original");
    }
}
