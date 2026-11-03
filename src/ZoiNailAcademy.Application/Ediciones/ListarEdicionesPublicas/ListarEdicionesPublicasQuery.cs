using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Ediciones.ListarEdicionesPublicas;

public sealed record ListarEdicionesPublicasQuery(string Slug) : IRequest<Result<IReadOnlyList<EdicionPublicaDto>>>;
