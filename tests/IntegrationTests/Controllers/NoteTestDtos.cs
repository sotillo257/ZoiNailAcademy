namespace IntegrationTests.Controllers;

public record NoteResponseDto(Guid Id, string Title, string Content, DateTime CreatedAt, DateTime? UpdatedAt);

public record PagedNotesResponseDto(List<NoteResponseDto> Items, int Page, int PageSize, int TotalCount);

public record ValidationProblemDetailsDto(string? Title, int? Status, Dictionary<string, string[]> Errors);
