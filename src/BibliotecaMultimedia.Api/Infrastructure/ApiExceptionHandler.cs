using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaMultimedia.Api.Infrastructure;

public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status=500;
        var title="Ocurrió un error interno. Intenta nuevamente más tarde.";
        if(exception is DbUpdateException { InnerException: SqlException sql } && sql.Number is 2601 or 2627 or 547)
        {
            status=400;title="La operación viola una relación o una restricción de unicidad. Revisa los datos.";
        }
        else if(exception is DbUpdateConcurrencyException)
        {
            status=409;title="El recurso cambió durante la operación. Vuelve a consultarlo.";
        }
        if(status==500) logger.LogError(exception,"Error interno. Correlación {TraceId}",context.TraceIdentifier);
        context.Response.StatusCode=status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails{Status=status,Title=title,
            Extensions={ ["traceId"]=context.TraceIdentifier }},cancellationToken);
        return true;
    }
}
