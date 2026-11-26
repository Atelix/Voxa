namespace Voxa.Abstractions;

/// <summary>Too many failed attempts from this client; it has to wait. Reported as HTTP 429 with Retry-After.</summary>
public class DomainRateLimitedException : Exception
{
    public DomainRateLimitedException()
    {
    }

    public DomainRateLimitedException(string message) : base(message)
    {
    }

    public DomainRateLimitedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public DomainRateLimitedException(TimeSpan retryAfter) : base("Too many failed attempts. Try again later.") => RetryAfter = retryAfter;

    public TimeSpan RetryAfter { get; }
}
