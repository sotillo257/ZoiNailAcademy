using FluentAssertions;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Notes.GetNote;
using ZoiNailAcademy.Domain.Entities;
using MockQueryable.Moq;
using Moq;

namespace UnitTests.Application.Notes.GetNote;

public class GetNoteHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContext = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly GetNoteHandler _handler;

    public GetNoteHandlerTests()
    {
        _currentUserService.Setup(x => x.GetUserId()).Returns(_userId);
        _handler = new GetNoteHandler(_dbContext.Object, _currentUserService.Object);
    }

    private void SetupNotes(params Note[] notes)
        => _dbContext.Setup(x => x.Notes).Returns(notes.ToList().BuildMockDbSet().Object);

    [Fact]
    public async Task Handle_OwnNote_ReturnsDto()
    {
        var note = Note.Create(_userId, "Ideas", "Contenido");
        SetupNotes(note);

        var result = await _handler.Handle(new GetNoteQuery(note.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(note.Id);
        result.Value.Title.Should().Be("Ideas");
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        SetupNotes();

        var act = async () => await _handler.Handle(new GetNoteQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ForeignNote_ThrowsNotFoundException()
    {
        var foreign = Note.Create(Guid.NewGuid(), "Ajena", "");
        SetupNotes(foreign);

        var act = async () => await _handler.Handle(new GetNoteQuery(foreign.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
