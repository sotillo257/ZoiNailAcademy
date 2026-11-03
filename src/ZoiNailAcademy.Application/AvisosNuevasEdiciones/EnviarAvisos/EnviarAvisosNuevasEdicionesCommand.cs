using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.AvisosNuevasEdiciones.EnviarAvisos;

/// <summary>Notifies the waiting lists of the courses with newly published dates. Returns how many people were emailed.</summary>
public sealed record EnviarAvisosNuevasEdicionesCommand : IRequest<Result<int>>;
