using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.ListasEspera.ExportarListaEspera;

/// <param name="CursoId">Null exports every course.</param>
public sealed record ExportarListaEsperaQuery(Guid? CursoId) : IRequest<Result<ArchivoCsv>>;

public sealed record ArchivoCsv(string NombreArchivo, byte[] Contenido);
