using System.Globalization;
using Voxa.Abstractions;
using Voxa.Abstractions.Models;
using Voxa.Persistence.Entity;
using Voxa.Services;

namespace Voxa.WebApi.UnitTests.Logic;

/// <summary>Every language has every text, and mails go out in the meeting's language, else the club's.</summary>
public sealed class LanguageTests : IDisposable
{
    private static readonly DateTime s_meetingStart = new(2027, 2, 2, 17, 45, 0, DateTimeKind.Utc);
    private readonly TestServices _services = new(new DateTimeOffset(2027, 2, 1, 9, 0, 0, TimeSpan.Zero));

    public void Dispose() => _services.Dispose();

    public static TheoryData<string> SupportedLanguages() => [.. Languages.Supported];

    [Theory]
    [MemberData(nameof(SupportedLanguages))]
    public void EveryLanguageHasEveryText(string language)
    {
        var english = Texts.All(CultureInfo.GetCultureInfo("en"));
        var texts = Texts.All(CultureInfo.GetCultureInfo(language));

        Assert.Equal(english.Keys.Order(StringComparer.Ordinal), texts.Keys.Order(StringComparer.Ordinal));
        Assert.All(texts.Values, static text => Assert.False(string.IsNullOrWhiteSpace(text)));
    }

    [Fact]
    public void AnUnknownLanguageFallsBackToTheDefault() =>
        Assert.Equal(Languages.Default, Texts.Culture("xx").Name);

    [Fact]
    public void TheMailIsInTheClubsLanguage()
    {
        var mail = CodeMail.Build(Meeting(language: null), Club("en"), [new MailedCode(new Voter { Name = "Jeugd Een", GuardianName = "Ouder" }, "AAAA-BBBB")], "ouder@example.org", App());

        Assert.Equal("Your personal voting code for the ALV 2027", mail.Subject);
        Assert.Contains("Hi Ouder,", mail.Text, StringComparison.Ordinal);
        Assert.Contains("On Tuesday 2 February 2027, 18:45 in Kantine, Club holds the ALV 2027.", mail.Text, StringComparison.Ordinal);
        Assert.Contains("With this code you vote on behalf of Jeugd Een.", mail.Text, StringComparison.Ordinal);
        Assert.Contains("Your voting code: AAAA-BBBB", mail.Text, StringComparison.Ordinal);
        Assert.Contains("<html lang=\"en\">", mail.Html, StringComparison.Ordinal);
        Assert.Contains("<strong>Tuesday 2 February 2027, 18:45</strong>", mail.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("stemcode", mail.Html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheMeetingsLanguageWinsOverTheClubs()
    {
        var english = CodeMail.Build(Meeting("en"), Club("nl"), [new MailedCode(new Voter { Name = "Lid" }, "AAAA-BBBB")], "lid@example.org", App());
        var dutch = CodeMail.Build(Meeting(language: null), Club("nl"), [new MailedCode(new Voter { Name = "Lid" }, "AAAA-BBBB")], "lid@example.org", App());

        Assert.StartsWith("Hi Lid,", english.Text, StringComparison.Ordinal);
        Assert.StartsWith("Hoi Lid,", dutch.Text, StringComparison.Ordinal);
        Assert.Contains("dinsdag 2 februari 2027, 18:45", dutch.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void TextsInTheMailAreEncodedButTheirMarkupIsKept()
    {
        var mail = CodeMail.Build(Meeting("en"), Club("en"), [new MailedCode(new Voter { Name = "<b>Jan</b>" }, "AAAA-BBBB")], "lid@example.org", App());

        Assert.Contains("Hi &lt;b&gt;Jan&lt;/b&gt;,", mail.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>Jan</b>", mail.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AMeetingKeepsItsLanguageAndRefusesAnUnknownOne()
    {
        var meeting = await _services.Meetings.CreateAsync(new MeetingRequest { Name = "AGM 2027", StartsAt = s_meetingStart, Language = "en" }, CancellationToken.None);
        var copy = await _services.Meetings.CreateAsync(new MeetingRequest { Name = "ALV 2027", StartsAt = s_meetingStart }, CancellationToken.None);

        Assert.Equal("en", meeting.Language);
        Assert.Null(copy.Language);
        _ = await Assert.ThrowsAsync<DomainValidationException>(() =>
            _services.Meetings.CreateAsync(new MeetingRequest { Name = "AGM", StartsAt = s_meetingStart, Language = "xx" }, CancellationToken.None));
    }

    [Theory]
    [InlineData("WISSEN")]
    [InlineData("WIPE")]
    public async Task TheWipeWordCountsInEveryLanguage(string word) =>
        await _services.Meetings.WipeAsync(new WipeRequest { Confirm = word }, CancellationToken.None);

    [Fact]
    public async Task AnotherWordDoesNotWipe() =>
        _ = await Assert.ThrowsAsync<DomainValidationException>(() => _services.Meetings.WipeAsync(new WipeRequest { Confirm = "wissen" }, CancellationToken.None));

    private static Meeting Meeting(string? language) => new() { Name = "ALV 2027", StartsAt = s_meetingStart, Location = "Kantine", Language = language };

    private static ClubSettings Club(string language) => new()
    {
        ClubName = "Club",
        Language = language,
        PrimaryColor = "#231E5B",
        SecondaryColor = "#62CDF6",
        HeadingFont = "Oswald",
        BodyFont = "Hind",
        MailFromName = "Club",
    };

    private static AppOptions App() => new() { PublicUrl = "https://stem.example.org/" };
}
