namespace IntegrationTests.Controllers;

public record UserResponseDto(Guid Id, string Email, string DisplayName);

public record AuthResponseDto(string AccessToken, string RefreshToken, UserResponseDto User);

public record TokenPairResponseDto(string AccessToken, string RefreshToken);

public record ProtectedEndpointResponseDto(string? UserId);

public record ProblemDetailsDto(string? Title, string? Detail, int? Status);
