using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Csv;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.ListasEspera;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Reservas.ExportarAsistentes;

public class ExportarAsistentesHandler(IAppDbContext dbContext, IAppSettings appSettings)
    : IRequestHandler<ExportarAsistentesQuery, Result<ArchivoCsv>>
{
    public async Task<Result<ArchivoCsv>> Handle(ExportarAsistentesQuery request, CancellationToken cancellationToken)
    {
        var edicion = await dbContext.Ediciones
            .Where(e => e.Id == request.EdicionId)
            .Select(e => new { e.Inicio, e.Curso.Slug })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Edición", request.EdicionId);

        var asistentes = await dbContext.Reservas
            .Where(r => r.EdicionId == request.EdicionId && r.Estado == EstadoReserva.Pagada)
            .OrderBy(r => r.PagadaEn).ThenBy(r => r.Id)
            .Select(r => new { r.Codigo, r.Nombre, r.Telefono, r.Email, r.Metodo, r.Importe, r.PrecioCurso, r.PagadaEn })
            .ToListAsync(cancellationToken);

        var zona = appSettings.TimeZone;
        var csv = new CsvBuilder(
            "Código", "Nombre", "Teléfono", "Email", "Método", "Importe", "Resto en persona", "Fecha de pago");
        foreach (var a in asistentes)
        {
            csv.Fila(
                a.Codigo, a.Nombre, a.Telefono, a.Email, NombreMetodo(a.Metodo), FormatearImporte(a.Importe),
                FormatearImporte(a.PrecioCurso - a.Importe), ListaEsperaAdminDto.FormatearFecha(a.PagadaEn, zona));
        }

        var inicio = TimeZoneInfo.ConvertTime(edicion.Inicio, zona).ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        return Result.Success(new ArchivoCsv($"asistentes-{edicion.Slug}-{inicio}.csv", csv.ToBytes()));
    }

    public static string NombreMetodo(MetodoPago metodo) =>
        metodo == MetodoPago.Stripe ? "Tarjeta" : "Transferencia o Bizum";

    /// <summary>Decimal comma for Excel in Spanish (<c>149,00</c>), without depending on ICU culture data.</summary>
    public static string FormatearImporte(decimal importe) =>
        importe.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');
}
