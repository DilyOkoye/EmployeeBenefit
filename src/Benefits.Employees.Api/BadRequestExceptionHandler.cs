using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Benefits.Employees.Api;

/// <summary>
/// Safety net: any <see cref="BadHttpRequestException"/> that escapes (e.g. a body that is too large or a
/// connection that ends mid-request) is the client's fault, so answer with its 4xx status as problem+json
/// rather than letting it fall through to a 500.
/// </summary>
internal sealed class BadRequestExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException badRequest)
        {
            return false;
        }

        httpContext.Response.StatusCode = badRequest.StatusCode;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = "The request could not be processed.",
                Detail = badRequest.Message,
            },
        });
    }
}
