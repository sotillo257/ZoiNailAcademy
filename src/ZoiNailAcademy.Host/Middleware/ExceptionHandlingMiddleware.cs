using System.Net;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using ValidationException = ZoiNailAcademy.Application.Common.Exceptions.ValidationException;

namespace ZoiNailAcademy.Host.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                (ProblemDetails)new ValidationProblemDetails(validationEx.Errors)
                {
                    Title = "Se han producido uno o más errores de validación.",
                    Status = (int)HttpStatusCode.BadRequest
                }),

            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                CreateProblemDetails(HttpStatusCode.NotFound, notFoundEx.Message)),

            ConflictException conflictEx => (
                HttpStatusCode.Conflict,
                CreateProblemDetails(HttpStatusCode.Conflict, conflictEx.Message)),

            UnauthorizedException unauthorizedEx => (
                HttpStatusCode.Unauthorized,
                CreateProblemDetails(HttpStatusCode.Unauthorized, unauthorizedEx.Message)),

            ForbiddenException forbiddenEx => (
                HttpStatusCode.Forbidden,
                CreateProblemDetails(HttpStatusCode.Forbidden, forbiddenEx.Message)),

            DomainException domainEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails(HttpStatusCode.BadRequest, domainEx.Message)),

            _ => LogAndCreateInternalError(exception)
        };

        context.Response.StatusCode = (int)statusCode;

        // WriteAsJsonAsync ignores a pre-set ContentType and falls back to its own
        // "application/json" default unless the content type is passed explicitly here.
        // The runtime type is passed so derived types (ValidationProblemDetails) keep their
        // extra members ("errors") instead of being serialized as the declared ProblemDetails.
        await context.Response.WriteAsJsonAsync(
            problemDetails, problemDetails.GetType(), options: null, contentType: "application/problem+json");
    }

    private static ProblemDetails CreateProblemDetails(HttpStatusCode statusCode, string detail) => new()
    {
        Status = (int)statusCode,
        Title = statusCode.ToString(),
        Detail = detail
    };

    private (HttpStatusCode, ProblemDetails) LogAndCreateInternalError(Exception exception)
    {
        logger.LogError(exception, "Excepción no controlada");
        return (
            HttpStatusCode.InternalServerError,
            CreateProblemDetails(HttpStatusCode.InternalServerError, "Se ha producido un error interno del servidor."));
    }
}
