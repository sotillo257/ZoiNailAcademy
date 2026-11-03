using MediatR;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Csv;

namespace ZoiNailAcademy.Application.ListasEspera.ExportarListaEspera;

/// <param name="CursoId">Null exports every course.</param>
public sealed record ExportarListaEsperaQuery(Guid? CursoId) : IRequest<Result<ArchivoCsv>>;
