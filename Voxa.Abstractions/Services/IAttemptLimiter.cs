namespace Voxa.Abstractions.Services;

/// <summary>Counts failed attempts (wrong codes, wrong passwords) per client, in memory only.</summary>
public interface IAttemptLimiter
{
    /// <exception cref="DomainRateLimitedException">One of the keys has too many recent failures.</exception>
    void EnsureAllowed(IEnumerable<AttemptKey> keys);

    void RecordFailure(IEnumerable<AttemptKey> keys);
}

/// <summary>A client key with its own limit, such as a device (strict) or a network address shared by a hall (loose).</summary>
public sealed record AttemptKey(string Key, int MaxFailures);
