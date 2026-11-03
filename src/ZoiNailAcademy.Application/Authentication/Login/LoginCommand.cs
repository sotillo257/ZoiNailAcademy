using ZoiNailAcademy.Application.Common;
using MediatR;

namespace ZoiNailAcademy.Application.Authentication.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<AuthResult>>;
