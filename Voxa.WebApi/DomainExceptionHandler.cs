using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Voxa.Abstractions;

namespace Voxa.WebApi;

/// <summary>
/// Maps the domain exceptions to problem responses, so every endpoint reports a failure the same way:
/// 400 for invalid input, 403 for a refused action, 404 for a missing record, 409 for a conflict and 429 after too
/// many failed attempts.
/// </summary>
public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var problem = exception switch
        {
            ObjectNotFoundException notFound => NotFound(notFound),
            BadHttpRequestException badRequest => new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = "Bad request",
                Detail = badRequest.Message,
            },
            DomainValidationException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad request",
                Detail = exception.Message,
            },
            DomainForbiddenException => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Not allowed",
                Detail = exception.Message,
            },
            DomainRateLimitedException rateLimited => RateLimited(httpContext, rateLimited),
            DomainConflictException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "The change could not be applied",
                Detail = exception.Message,
            },
            _ => null,
        };

        if (problem == null)
        {
            return false;
        }

        problem.Instance = httpContext.Request.Path;
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, contentType: "application/problem+json", cancellationToken);
        return true;
    }

    private static ProblemDetails RateLimited(HttpContext httpContext, DomainRateLimitedException exception)
    {
        var seconds = Math.Max(1, (int)Math.Ceiling(exception.RetryAfter.TotalSeconds));
        httpContext.Response.Headers.RetryAfter = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many attempts",
            Detail = exception.Message,
        };
        problem.Extensions["retryAfterSeconds"] = seconds;
        return problem;
    }

    private static ProblemDetails NotFound(ObjectNotFoundException exception)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Resource not found",
            Detail = exception.Message,
        };
        if (exception.EntityName is not null)
        {
            problem.Extensions["entity"] = exception.EntityName;
        }

        if (exception.Identifier is not null)
        {
            problem.Extensions["identifier"] = exception.Identifier.ToString();
        }

        return problem;
    }
}
