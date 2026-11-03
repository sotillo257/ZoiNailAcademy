using ZoiNailAcademy.Application.Common;
using MediatR;

namespace ZoiNailAcademy.Application.Authentication.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<Result<UserDto>>;
