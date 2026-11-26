using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Voxa.Abstractions;
using Voxa.Services;

namespace Voxa.WebApi;

/// <summary>
/// Maps the domain exceptions to problem responses, so every endpoint reports a failure the same way:
/// 400 for invalid input, 403 for a refused action, 404 for a missing record, 409 for a conflict and 429 after too
/// many failed attempts. Titles and details are in the request's language (Accept-Language); the logic writes its
/// messages with <see cref="Texts"/>.
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
                Title = Texts.Get("Problem.BadRequest"),
                Detail = badRequest.Message,
            },
            DomainValidationException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = Texts.Get("Problem.BadRequest"),
                Detail = exception.Message,
            },
            DomainForbiddenException => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = Texts.Get("Problem.Forbidden"),
                Detail = exception.Message,
            },
            DomainRateLimitedException rateLimited => RateLimited(httpContext, rateLimited),
            DomainConflictException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = Texts.Get("Problem.Conflict"),
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
            Title = Texts.Get("Problem.TooManyAttempts"),
            Detail = Texts.Get("Error.TooManyAttempts"),
        };
        problem.Extensions["retryAfterSeconds"] = seconds;
        return problem;
    }

    private static ProblemDetails NotFound(ObjectNotFoundException exception)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = Texts.Get("Problem.NotFound"),
            // A lookup by id has an English message for the logs; people get the general text in their language.
            Detail = exception.EntityName is null ? exception.Message : Texts.Get("Error.NotFound"),
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
