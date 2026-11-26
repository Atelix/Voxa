namespace Voxa.Abstractions;

/// <summary>
/// The languages Voxa speaks, as two-letter codes. The API answers in the language the app asks for
/// (Accept-Language), and mails go out in the meeting's language, else the club's. To add a language, add its code
/// here, a <c>Texts.&lt;code&gt;.resx</c> next to <c>Voxa/Resources/Texts.resx</c> and a file in
/// <c>Voxa.PWA/src/languages</c> (docs/languages.md).
/// </summary>
public static class Languages
{
    /// <summary>The language of a new club and of requests that ask for none Voxa knows: Voxa is made for Dutch clubs.</summary>
    public const string Default = "nl";

    public static readonly IReadOnlyList<string> Supported = ["nl", "en"];

    public static bool IsSupported(string? code) => code is not null && Supported.Contains(code, StringComparer.Ordinal);
}
