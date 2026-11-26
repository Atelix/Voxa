using System.Globalization;
using System.Resources;
using Voxa.Abstractions;

// Texts.resx holds the English texts, so English needs no satellite assembly.
[assembly: NeutralResourcesLanguage("en")]

namespace Voxa.Services;

/// <summary>
/// The texts the API shows to people: error messages, mails and trial data. English lives in
/// <c>Resources/Texts.resx</c>, every other language in <c>Texts.&lt;code&gt;.resx</c> (see <see cref="Languages"/>).
/// Without a culture the text follows the request (<see cref="CultureInfo.CurrentUICulture"/>, set from Accept-Language).
/// </summary>
public static class Texts
{
    private static readonly ResourceManager s_resources = new("Voxa.Resources.Texts", typeof(Texts).Assembly);

    /// <summary>The culture for a language code, or the default language when Voxa does not know the code.</summary>
    public static CultureInfo Culture(string? language) => CultureInfo.GetCultureInfo(Languages.IsSupported(language) ? language! : Languages.Default);

    /// <summary>The text for a key in the request's language.</summary>
    public static string Get(string key) => GetIn(CultureInfo.CurrentUICulture, key);

    /// <summary>The text for a key in the request's language, with its placeholders filled in.</summary>
    public static string Format(string key, params object?[] args) => FormatIn(CultureInfo.CurrentUICulture, key, args);

    /// <summary>The text for a key in a given language. A missing key is a bug, so it throws instead of showing the key.</summary>
    public static string GetIn(CultureInfo culture, string key) =>
        s_resources.GetString(key, culture) ?? throw new InvalidOperationException($"There is no text '{key}'.");

    public static string FormatIn(CultureInfo culture, string key, params object?[] args) => string.Format(culture, GetIn(culture, key), args);

    /// <summary>Every key with its text in one language, to check that each language has every text.</summary>
    public static IReadOnlyDictionary<string, string> All(CultureInfo culture)
    {
        var set = s_resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)
                  ?? throw new InvalidOperationException($"There are no texts for '{culture.Name}'.");
        return set.Cast<System.Collections.DictionaryEntry>().ToDictionary(static e => (string)e.Key, static e => (string)e.Value!, StringComparer.Ordinal);
    }
}
