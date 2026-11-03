using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Application.Emails;
using ZoiNailAcademy.Application.Emails.Plantillas;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.AvisosNuevasEdiciones.EnviarAvisos;

/// <summary>
/// For each course marked by <c>MarcarCursoPendienteHandler</c>: one email per person of its waiting list (subscribed)
/// with every bookable date that has free seats and was not notified to them yet, plus a summary for the client.
/// The <see cref="AvisoEnviado"/> rows, the emails and the removal of the mark are saved together, so a failure retries
/// the whole course next cycle and nobody gets the same date twice. Emails are enqueued in batches of
/// <c>Avisos:LoteTamano</c>, each one due <c>Avisos:PausaEntreLotesSegundos</c> after the previous one. Runs behind an
/// advisory lock, so several replicas never notify the same course twice.
/// </summary>
public class EnviarAvisosNuevasEdicionesHandler(
    IAppDbContext dbContext, EdicionesPublicas edicionesPublicas, BandejaSalida bandeja, IEmailSettings emailSettings,
    IAvisosSettings avisosSettings, TimeProvider timeProvider, ILogger<EnviarAvisosNuevasEdicionesHandler> logger)
    : IRequestHandler<EnviarAvisosNuevasEdicionesCommand, Result<int>>
{
    /// <summary>Advisory lock key (the expiry job uses <c>…0001</c> and the outbox <c>…0002</c>).</summary>
    public const long ClaveLock = 0x5A4F_4900_0003;

    public async Task<Result<int>> Handle(EnviarAvisosNuevasEdicionesCommand request, CancellationToken cancellationToken)
    {
        await using var bloqueo = await dbContext.TryAdquirirLockAsync(ClaveLock, cancellationToken);
        if (bloqueo is null)
        {
            logger.LogInformation("Otra instancia está enviando los avisos de nuevas fechas; se salta este ciclo.");
            return Result.Success(0);
        }

        var pendientes = await dbContext.AvisosPendientes
            .OrderBy(p => p.PendienteDesde)
            .ToListAsync(cancellationToken);

        var avisados = 0;
        foreach (var pendiente in pendientes)
        {
            try
            {
                avisados += await AvisarCursoAsync(pendiente, avisados, cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // A date of this course was published while we were notifying it: next cycle includes it.
                logger.LogInformation("Se publicó otra fecha del curso {CursoId} mientras se avisaba; se reintenta en el próximo ciclo.",
                    pendiente.CursoId);
                dbContext.DescartarCambios();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The mark stays: the course is notified again next cycle.
                logger.LogError(ex, "No se pudo enviar el aviso de nuevas fechas del curso {CursoId}.", pendiente.CursoId);
                dbContext.DescartarCambios();
            }
        }

        return Result.Success(avisados);
    }

    /// <param name="yaEncolados">Notices enqueued in this cycle for earlier courses (to continue their batches).</param>
    /// <returns>People emailed.</returns>
    private async Task<int> AvisarCursoAsync(AvisoPendiente pendiente, int yaEncolados, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var curso = await dbContext.Cursos.AsNoTracking()
            .Where(c => c.Id == pendiente.CursoId)
            .Select(c => new { c.Nombre, c.Slug })
            .SingleAsync(cancellationToken);

        // Bookable now (scheduled, course active, outside the closing window) and with free seats.
        var publicas = await edicionesPublicas.PorCursoAsync([pendiente.CursoId], cancellationToken);
        var fechas = (publicas.TryGetValue(pendiente.CursoId, out var lista) ? lista : [])
            .Where(e => e.PlazasLibres > 0)
            .ToList();

        var avisados = 0;
        var fechasAvisadas = new Dictionary<Guid, FechaAvisoModelo>();
        if (fechas.Count > 0)
        {
            var edicionIds = fechas.Select(e => e.Id).ToList();
            var personas = await dbContext.ListaEspera.AsNoTracking()
                .Where(l => l.CursoId == pendiente.CursoId && l.BajaEn == null)
                .OrderBy(l => l.CreatedAt).ThenBy(l => l.Id)
                .Select(l => new { l.Id, l.Nombre, l.Email, l.BajaToken })
                .ToListAsync(cancellationToken);
            var yaAvisadas = (await dbContext.AvisosEnviados.AsNoTracking()
                    .Where(a => edicionIds.Contains(a.EdicionId))
                    .Select(a => new { a.ListaEsperaId, a.EdicionId })
                    .ToListAsync(cancellationToken))
                .Select(a => (a.ListaEsperaId, a.EdicionId))
                .ToHashSet();

            var marca = bandeja.Marca();
            foreach (var persona in personas)
            {
                var nuevas = fechas.Where(e => !yaAvisadas.Contains((persona.Id, e.Id))).ToList();
                if (nuevas.Count == 0)
                {
                    continue;
                }

                var modelos = nuevas.Select(e => new FechaAvisoModelo(e.Inicio, e.PlazasLibres)).ToList();
                var contenido = AvisoNuevasEdicionesEmail.Crear(
                    new AvisoNuevasEdicionesModelo(curso.Nombre, curso.Slug, persona.Nombre, modelos, persona.BajaToken), marca);
                var lote = (yaEncolados + avisados) / avisosSettings.LoteTamano;

                dbContext.EmailsSalientes.Add(EmailSaliente.Create(
                    persona.Email, emailSettings.ReplyTo, contenido.Asunto, contenido.Html, contenido.Texto,
                    TiposEmail.AvisoNuevasEdiciones, AvisoReferencia.Calcular(persona.Id, nuevas.Select(e => e.Id)),
                    now + avisosSettings.PausaEntreLotes * lote,
                    AvisoNuevasEdicionesEmail.UrlBajaUnClic(marca, persona.BajaToken)));
                foreach (var edicion in nuevas)
                {
                    dbContext.AvisosEnviados.Add(AvisoEnviado.Create(persona.Id, edicion.Id, now));
                    fechasAvisadas.TryAdd(edicion.Id, new FechaAvisoModelo(edicion.Inicio, edicion.PlazasLibres));
                }

                avisados++;
            }

            if (avisados > 0 && emailSettings.AvisosClienta is { } avisosClienta)
            {
                var resumen = AvisoNuevasEdicionesClientaEmail.Crear(
                    new AvisoNuevasEdicionesClientaModelo(curso.Nombre, avisados, fechasAvisadas.Values.ToList()), marca);
                dbContext.EmailsSalientes.Add(EmailSaliente.Create(
                    avisosClienta, null, resumen.Asunto, resumen.Html, resumen.Texto,
                    TiposEmail.AvisoNuevasEdicionesClienta, null, now));
            }
        }

        dbContext.AvisosPendientes.Remove(pendiente);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (avisados > 0)
        {
            logger.LogInformation("Aviso enviado a {Avisados} interesados · {Curso}", avisados, curso.Nombre);
        }

        return avisados;
    }
}
