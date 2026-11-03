using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Contacto;
using ZoiNailAcademy.Application.Common.Events;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.ListasEspera.AltaListaEspera;

/// <summary>
/// Joins the waiting list of an active course. The same email on the same course updates the existing entry
/// (and the answer is identical, so the form never reveals whether someone was already on the list).
/// </summary>
public class AltaListaEsperaHandler(
    IAppDbContext dbContext, IPublisher publisher, TimeProvider timeProvider, ILogger<AltaListaEsperaHandler> logger)
    : IRequestHandler<AltaListaEsperaCommand, Result>
{
    public async Task<Result> Handle(AltaListaEsperaCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.Website))
        {
            logger.LogWarning("Honeypot relleno en la lista de espera; se ignora la petición.");
            return Result.Success();
        }

        var curso = await dbContext.Cursos.FirstOrDefaultAsync(c => c.Id == request.CursoId && c.Activo, cancellationToken)
            ?? throw new NotFoundException("Curso", request.CursoId);
        var edicion = await EdicionPreferidaAsync(request, cancellationToken);

        if (!TelefonoNormalizer.TryNormalizar(request.Telefono, out var telefono))
        {
            throw new ValidationException([new ValidationFailure(nameof(request.Telefono), "El teléfono no es válido.")]);
        }

        var email = TelefonoNormalizer.NormalizarEmail(request.Email);
        ListaEspera entrada;
        try
        {
            entrada = await GuardarAsync(curso, edicion, request, email, telefono, cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Two simultaneous sign-ups of the same person: the unique index (CursoId, Email) rejected the
            // second insert, so it becomes an update of the entry the first one created.
            logger.LogInformation(ex, "Alta simultánea en la lista de espera del curso {CursoId}; se actualiza.", curso.Id);
            dbContext.DescartarCambios();
            curso = await dbContext.Cursos.FirstAsync(c => c.Id == request.CursoId, cancellationToken);
            edicion = await EdicionPreferidaAsync(request, cancellationToken);
            entrada = await GuardarAsync(curso, edicion, request, email, telefono, cancellationToken);
        }

        await publisher.PublicarTrasCommitAsync(new ListaEsperaAltaEvent(entrada.Id), logger, cancellationToken);
        return Result.Success();
    }

    private async Task<ListaEspera> GuardarAsync(
        Curso curso, Edicion? edicion, AltaListaEsperaCommand request, string email, string telefono,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var entrada = await dbContext.ListaEspera
            .FirstOrDefaultAsync(e => e.CursoId == curso.Id && e.Email == email, cancellationToken);

        if (entrada is null)
        {
            entrada = ListaEspera.Create(
                curso, edicion, request.FechaPreferida, request.Nombre, email, telefono, ConsentimientoListaEspera.Vigente, now);
            dbContext.ListaEspera.Add(entrada);
        }
        else
        {
            entrada.ActualizarAlta(
                edicion, request.FechaPreferida, request.Nombre, telefono, ConsentimientoListaEspera.Vigente, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return entrada;
    }

    private async Task<Edicion?> EdicionPreferidaAsync(AltaListaEsperaCommand request, CancellationToken cancellationToken)
    {
        if (request.EdicionPreferidaId is not { } edicionId)
        {
            return null;
        }

        return await dbContext.Ediciones.FirstOrDefaultAsync(
                e => e.Id == edicionId && e.CursoId == request.CursoId, cancellationToken)
            ?? throw new ValidationException(
                [new ValidationFailure(nameof(request.EdicionPreferidaId), "La fecha elegida no pertenece a este curso.")]);
    }
}
