namespace Voxa.Abstractions.Services;

/// <summary>Creates voting codes and the keyed hash that is the only form in which a code is stored.</summary>
public interface ICodeService
{
    /// <summary>A new random code, formatted as XXXX-XXXX from an alphabet without 0, O, 1 and I.</summary>
    string Generate();

    /// <summary>The code without dashes, spaces or case, or null when it cannot be a code.</summary>
    string? Normalize(string? code);

    /// <summary>The keyed hash (HMAC-SHA256 with the server's pepper) of a normalised code.</summary>
    string Hash(string normalizedCode);
}
