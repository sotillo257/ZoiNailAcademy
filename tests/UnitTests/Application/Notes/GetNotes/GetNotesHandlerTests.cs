using FluentAssertions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Notes.GetNotes;
using ZoiNailAcademy.Domain.Entities;
using MockQueryable.Moq;
using Moq;

namespace UnitTests.Application.Notes.GetNotes;

public class GetNotesHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContext = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly GetNotesHandler _handler;

    public GetNotesHandlerTests()
    {
        _currentUserService.Setup(x => x.GetUserId()).Returns(_userId);
        _handler = new GetNotesHandler(_dbContext.Object, _currentUserService.Object);
    }

    private void SetupNotes(params Note[] notes)
        => _dbContext.Setup(x => x.Notes).Returns(notes.ToList().BuildMockDbSet().Object);

    private Note NoteCreatedAt(Guid userId, string title, DateTime createdAt)
    {
        var note = Note.Create(userId, title, "");
        note.CreatedAt = createdAt;
        return note;
    }

    [Fact]
    public async Task Handle_OnlyCurrentUsersNotesReturned()
    {
        var mine = NoteCreatedAt(_userId, "Mía", DateTime.UtcNow);
        var foreign = NoteCreatedAt(Guid.NewGuid(), "Ajena", DateTime.UtcNow);
        SetupNotes(mine, foreign);

        var result = await _handler.Handle(new GetNotesQuery(1, 20), CancellationToken.None);

        result.Value.Items.Should().ContainSingle(n => n.Id == mine.Id);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_OrdersByCreatedAtDescending()
    {
        var older = NoteCreatedAt(_userId, "Antigua", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = NoteCreatedAt(_userId, "Reciente", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        SetupNotes(older, newer);

        var result = await _handler.Handle(new GetNotesQuery(1, 20), CancellationToken.None);

        result.Value.Items.Select(n => n.Id).Should().Equal(newer.Id, older.Id);
    }

    [Fact]
    public async Task Handle_SecondPage_SkipsFirstPageAndReportsTotals()
    {
        var baseDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var notes = Enumerable.Range(0, 5)
            .Select(i => NoteCreatedAt(_userId, $"Nota {i}", baseDate.AddDays(i)))
            .ToArray();
        SetupNotes(notes);

        var result = await _handler.Handle(new GetNotesQuery(2, 2), CancellationToken.None);

        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(2);
        result.Value.TotalCount.Should().Be(5);
        result.Value.Items.Select(n => n.Title).Should().Equal("Nota 2", "Nota 1");
    }
}
