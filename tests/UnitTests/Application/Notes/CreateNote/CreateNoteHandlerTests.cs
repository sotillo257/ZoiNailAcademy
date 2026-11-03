using FluentAssertions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Notes.CreateNote;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;
using MockQueryable.Moq;
using Moq;

namespace UnitTests.Application.Notes.CreateNote;

public class CreateNoteHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContext = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly CreateNoteHandler _handler;

    public CreateNoteHandlerTests()
    {
        _currentUserService.Setup(x => x.GetUserId()).Returns(_userId);
        _dbContext.Setup(x => x.Notes).Returns(new List<Note>().BuildMockDbSet().Object);
        _handler = new CreateNoteHandler(_dbContext.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_AddsNoteForCurrentUserAndSaves()
    {
        Note? addedNote = null;
        _dbContext.Setup(x => x.Notes.Add(It.IsAny<Note>()))
            .Callback<Note>(n => addedNote = n);

        var result = await _handler.Handle(new CreateNoteCommand("Ideas", "Contenido"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Ideas");
        result.Value.Content.Should().Be("Contenido");
        addedNote.Should().NotBeNull();
        addedNote!.UserId.Should().Be(_userId);
        result.Value.Id.Should().Be(addedNote.Id);
        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithBlankTitle_ThrowsDomainExceptionAndDoesNotSave()
    {
        var act = async () => await _handler.Handle(new CreateNoteCommand("  ", "Contenido"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
