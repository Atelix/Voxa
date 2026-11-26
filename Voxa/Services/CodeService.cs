using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Voxa.Abstractions.Services;

namespace Voxa.Services;

public sealed class CodeOptions
{
    public const string Section = "Codes";

    /// <summary>
    /// The secret key of the code hash. Without it a stolen database cannot be used to try every possible code
    /// offline. Required outside Development.
    /// </summary>
    public string? Pepper { get; set; }
}

public sealed class CodeService(IOptions<CodeOptions> options) : ICodeService
{
    /// <summary>32 characters without 0, O, 1 and I, so a code read aloud or typed over cannot be confused.</summary>
    public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Eight characters of 5 bits: 40 bits, about a trillion possible codes.</summary>
    public const int Length = 8;

    private const string DevelopmentPepper = "development-only-pepper";

    private readonly byte[] _key = Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(options.Value.Pepper) ? DevelopmentPepper : options.Value.Pepper);

    public string Generate()
    {
        Span<char> code = stackalloc char[Length];
        for (var i = 0; i < Length; i++)
        {
            code[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return string.Concat(code[..4], "-", code[4..]);
    }

    public string? Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var builder = new StringBuilder(Length);
        foreach (var character in code)
        {
            if (character is '-' or ' ')
            {
                continue;
            }

            var upper = char.ToUpperInvariant(character);
            if (!Alphabet.Contains(upper, StringComparison.Ordinal))
            {
                return null;
            }

            _ = builder.Append(upper);
        }

        return builder.Length == Length ? builder.ToString() : null;
    }

    public string Hash(string normalizedCode)
    {
        ArgumentNullException.ThrowIfNull(normalizedCode);
        var hash = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(normalizedCode));
        return Convert.ToHexStringLower(hash);
    }
}
