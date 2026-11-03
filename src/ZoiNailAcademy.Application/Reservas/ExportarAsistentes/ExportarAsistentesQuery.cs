using MediatR;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Csv;

namespace ZoiNailAcademy.Application.Reservas.ExportarAsistentes;

/// <summary>Attendee list of an edition (paid bookings only) as a CSV for Excel.</summary>
public sealed record ExportarAsistentesQuery(Guid EdicionId) : IRequest<Result<ArchivoCsv>>;
