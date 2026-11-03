using FluentAssertions;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Notes.UpdateNote;
using ZoiNailAcademy.Domain.Entities;
using MockQueryable.Moq;
using Moq;

namespace UnitTests.Application.Notes.UpdateNote;

public class UpdateNoteHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContext = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly UpdateNoteHandler _handler;

    public UpdateNoteHandlerTests()
    {
        _currentUserService.Setup(x => x.GetUserId()).Returns(_userId);
        _handler = new UpdateNoteHandler(_dbContext.Object, _currentUserService.Object);
    }

    private void SetupNotes(params Note[] notes)
        => _dbContext.Setup(x => x.Notes).Returns(notes.ToList().BuildMockDbSet().Object);

    [Fact]
    public async Task Handle_OwnNote_UpdatesAndSaves()
    {
        var note = Note.Create(_userId, "Original", "Texto original");
        SetupNotes(note);

        var result = await _handler.Handle(new UpdateNoteCommand(note.Id, "Editada", "Texto editado"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Editada");
        result.Value.Content.Should().Be("Texto editado");
        note.Title.Should().Be("Editada");
        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        SetupNotes();

        var act = async () => await _handler.Handle(new UpdateNoteCommand(Guid.NewGuid(), "Editada", ""), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ForeignNote_ThrowsNotFoundExceptionAndDoesNotModify()
    {
        var foreign = Note.Create(Guid.NewGuid(), "Ajena", "Texto");
        SetupNotes(foreign);

        var act = async () => await _handler.Handle(new UpdateNoteCommand(foreign.Id, "Editada", ""), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        foreign.Title.Should().Be("Ajena");
        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
