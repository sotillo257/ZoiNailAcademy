using FluentAssertions;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Notes.DeleteNote;
using ZoiNailAcademy.Domain.Entities;
using MockQueryable.Moq;
using Moq;

namespace UnitTests.Application.Notes.DeleteNote;

public class DeleteNoteHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContext = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DeleteNoteHandler _handler;

    public DeleteNoteHandlerTests()
    {
        _currentUserService.Setup(x => x.GetUserId()).Returns(_userId);
        _handler = new DeleteNoteHandler(_dbContext.Object, _currentUserService.Object);
    }

    private Mock<Microsoft.EntityFrameworkCore.DbSet<Note>> SetupNotes(params Note[] notes)
    {
        var mockSet = notes.ToList().BuildMockDbSet();
        _dbContext.Setup(x => x.Notes).Returns(mockSet.Object);
        return mockSet;
    }

    [Fact]
    public async Task Handle_OwnNote_RemovesAndSaves()
    {
        var note = Note.Create(_userId, "Ideas", "");
        var mockSet = SetupNotes(note);

        var result = await _handler.Handle(new DeleteNoteCommand(note.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        mockSet.Verify(x => x.Remove(note), Times.Once);
        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ForeignNote_ThrowsNotFoundExceptionAndDoesNotRemove()
    {
        var foreign = Note.Create(Guid.NewGuid(), "Ajena", "");
        var mockSet = SetupNotes(foreign);

        var act = async () => await _handler.Handle(new DeleteNoteCommand(foreign.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        mockSet.Verify(x => x.Remove(It.IsAny<Note>()), Times.Never);
    }
}
