using ZoiNailAcademy.Application.Common;
using MediatR;

namespace ZoiNailAcademy.Application.Authentication.ChangePassword;

/// <param name="CurrentRefreshToken">Raw refresh token of the session making the change (from the <c>rt</c> cookie); that session is kept.</param>
public sealed record ChangePasswordCommand(string PasswordActual, string PasswordNueva, string? CurrentRefreshToken)
    : IRequest<Result>;
