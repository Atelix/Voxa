namespace Voxa.Abstractions.Services;

/// <summary>Who is calling, as far as rate limiting needs to know. Nothing of it is stored.</summary>
public interface IClientIdentity
{
    /// <summary>The client's network address (behind Cloudflare: CF-Connecting-IP).</summary>
    string Address { get; }

    /// <summary>A random id the app keeps per browser, or the address when it sends none.</summary>
    string Device { get; }
}
