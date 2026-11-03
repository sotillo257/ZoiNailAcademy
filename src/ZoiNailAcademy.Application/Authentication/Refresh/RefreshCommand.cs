using ZoiNailAcademy.Application.Common;
using MediatR;

namespace ZoiNailAcademy.Application.Authentication.Refresh;

public sealed record RefreshCommand(string RefreshToken) : IRequest<Result<TokenPairResponse>>;
