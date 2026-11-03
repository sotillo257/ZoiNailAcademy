using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Csv;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Application.ListasEspera.ExportarListaEspera;

/// <summary>The waiting list as a CSV for Excel, in the same order as the listing.</summary>
public class ExportarListaEsperaHandler(IAppDbContext dbContext, IAppSettings appSettings, TimeProvider timeProvider)
    : IRequestHandler<ExportarListaEsperaQuery, Result<ArchivoCsv>>
{
    public const string SlugTodos = "todos";

    public async Task<Result<ArchivoCsv>> Handle(ExportarListaEsperaQuery request, CancellationToken cancellationToken)
    {
        var slug = SlugTodos;
        if (request.CursoId is { } cursoId)
        {
            slug = await dbContext.Cursos.Where(c => c.Id == cursoId).Select(c => c.Slug).FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Curso", cursoId);
        }

        var zona = appSettings.TimeZone;
        var entradas = await ListaEsperaConsulta.Ordenar(ListaEsperaConsulta.Filtrar(dbContext.ListaEspera, request.CursoId))
            .ToListAsync(cancellationToken);

        var csv = new CsvBuilder(
            "Curso", "Nombre", "Teléfono", "Email", "Fecha preferida", "Edición preferida", "Contactado", "Fecha de alta",
            "Notas", "Baja");
        foreach (var e in entradas)
        {
            csv.Fila(
                e.Curso.Nombre, e.Nombre, e.Telefono, e.Email, e.FechaPreferida,
                ListaEsperaAdminDto.FormatearFecha(e.EdicionPreferida?.Inicio, zona), SiNo(e.Contactado),
                ListaEsperaAdminDto.FormatearFecha(e.CreatedAt, zona), e.Notas, SiNo(e.BajaEn is not null));
        }

        var hoy = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), zona).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        return Result.Success(new ArchivoCsv($"lista-espera-{slug}-{hoy}.csv", csv.ToBytes()));
    }

    private static string SiNo(bool valor) => valor ? "Sí" : "No";
}
