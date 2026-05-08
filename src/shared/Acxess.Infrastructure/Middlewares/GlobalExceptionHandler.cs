using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Acxess.Infrastructure.Middlewares;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error no controlado: {Message}", exception.Message);

        if (!IsApiRequest(httpContext)) return true;
        
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Error del Servidor",
            Detail = "Ocurrió un error interno al procesar su solicitud.",
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;

    }

    private static bool IsApiRequest(HttpContext httpContext)
    {
        // Verifica si el cliente pidió JSON explícitamente o si es una llamada AJAX (XHR)
        return httpContext.Request.Headers.Accept.ToString().Contains("application/json") ||
               httpContext.Request.Headers.XRequestedWith == "XMLHttpRequest";
    }
}
