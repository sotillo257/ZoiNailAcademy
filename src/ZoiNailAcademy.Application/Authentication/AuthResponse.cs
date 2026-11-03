namespace ZoiNailAcademy.Application.Authentication;

public sealed record UserDto(Guid Id, string Email, string DisplayName);

public sealed record AuthResponse(string AccessToken, string RefreshToken, UserDto User);
