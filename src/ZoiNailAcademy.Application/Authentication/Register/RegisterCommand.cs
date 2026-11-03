using ZoiNailAcademy.Application.Common;
using MediatR;

namespace ZoiNailAcademy.Application.Authentication.Register;

public sealed record RegisterCommand(string Email, string Password, string DisplayName)
    : IRequest<Result<AuthResponse>>;
