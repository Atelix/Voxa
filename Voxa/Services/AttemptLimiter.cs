using Microsoft.Extensions.Caching.Memory;
using Voxa.Abstractions;
using Voxa.Abstractions.Services;

namespace Voxa.Services;

/// <summary>
/// Failed attempts per key in a sliding window of <see cref="Window"/>, kept in memory only: a restart forgets them,
/// and nothing about the client is written to disk.
/// </summary>
public sealed class AttemptLimiter(IMemoryCache cache, TimeProvider time) : IAttemptLimiter
{
    /// <summary>How long failures count, and so how long a blocked client waits.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    private readonly Lock _lock = new();

    public void EnsureAllowed(IEnumerable<AttemptKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        var now = time.GetUtcNow();
        lock (_lock)
        {
            foreach (var key in keys)
            {
                var failures = Recent(key.Key, now);
                if (failures.Count >= key.MaxFailures)
                {
                    throw new DomainRateLimitedException(failures[0] + Window - now);
                }
            }
        }
    }

    public void RecordFailure(IEnumerable<AttemptKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        var now = time.GetUtcNow();
        lock (_lock)
        {
            foreach (var key in keys)
            {
                var failures = Recent(key.Key, now);
                failures.Add(now);
                _ = cache.Set(CacheKey(key.Key), failures, now + Window);
            }
        }
    }

    private List<DateTimeOffset> Recent(string key, DateTimeOffset now)
    {
        var failures = cache.TryGetValue<List<DateTimeOffset>>(CacheKey(key), out var stored) && stored is not null ? stored : [];
        _ = failures.RemoveAll(at => at + Window <= now);
        return failures;
    }

    private static string CacheKey(string key) => "attempts:" + key;
}
