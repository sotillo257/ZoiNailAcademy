using ZoiNailAcademy.Application.Common;
using MediatR;

namespace ZoiNailAcademy.Application.Authentication.Refresh;

/// <param name="RefreshToken">Raw refresh token read from the <c>rt</c> cookie; null when the cookie is missing.</param>
public sealed record RefreshCommand(string? RefreshToken) : IRequest<Result<AuthResult>>;
