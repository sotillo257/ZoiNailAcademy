using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Ediciones.BorrarEdicion;

public sealed record BorrarEdicionCommand(Guid Id) : IRequest<Result>;
