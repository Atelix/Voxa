using System.Globalization;
using System.Net;
using Voxa.Abstractions.Services;
using Voxa.Persistence.Entity;

namespace Voxa.Services;

/// <summary>A code in a mail, with the voter it is for. The voter is only used for the text of the mail.</summary>
public sealed record MailedCode(Voter? Voter, string Code);

/// <summary>
/// The mail with voting codes, in the club's colours (design: the "E-mail met stemcode" mockup) and in the meeting's
/// language, else the club's (<see cref="Language"/>). An address that serves several voters, such as a parent of
/// several youth members, gets one mail with every code and one link that opens the voting page with all of them.
/// </summary>
public static class CodeMail
{
    /// <summary>The language a meeting's mails are written in.</summary>
    public static CultureInfo Language(Meeting meeting, ClubSettings club)
    {
        ArgumentNullException.ThrowIfNull(meeting);
        ArgumentNullException.ThrowIfNull(club);
        return Texts.Culture(meeting.Language ?? club.Language);
    }

    public static OutgoingMail Build(Meeting meeting, ClubSettings club, IReadOnlyList<MailedCode> codes, string to, AppOptions app)
    {
        ArgumentNullException.ThrowIfNull(meeting);
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(club);
        ArgumentNullException.ThrowIfNull(codes);
        if (codes.Count == 0)
        {
            throw new ArgumentException("A code mail needs at least one code.", nameof(codes));
        }

        var culture = Language(meeting, club);
        string T(string key) => Texts.GetIn(culture, key);
        string F(string key, params object?[] args) => Texts.FormatIn(culture, key, args);

        var several = codes.Count > 1;
        var link = $"{app.PublicUrl.TrimEnd('/')}/#code={string.Join(',', codes.Select(static c => c.Code.Replace("-", string.Empty, StringComparison.Ordinal)))}";
        var voters = codes.Select(static c => c.Voter).ToList();
        var member = T("Mail.Member");
        var greeting = voters.Select(static v => v?.GuardianName).FirstOrDefault(static g => g is not null) ?? voters[0]?.Name ?? member;
        var onBehalf = !several && voters[0]?.GuardianName is not null ? voters[0]!.Name : null;
        var when = ClubTime.FormatDateTime(meeting.StartsAt, culture);
        var advance = meeting.AdvanceVoting && meeting.AdvanceOpensAt is { } opens
            ? F("Mail.Advance", ClubTime.FormatDate(opens, culture))
            : T("Mail.DuringMeeting");
        var subject = F(several ? "Mail.SubjectSeveral" : "Mail.SubjectOne", meeting.Name);
        var location = meeting.Location is null ? string.Empty : F("Mail.Location", meeting.Location);
        var intro = F("Mail.Intro", when, location, club.ClubName, meeting.Name);
        var severalText = F("Mail.Several", codes.Count);
        var keep = T(several ? "Mail.KeepSeveral" : "Mail.KeepOne");
        var secret = T("Mail.Secret");

        var lines = new List<string?> { F("Mail.Greeting", greeting), string.Empty, intro };
        if (several)
        {
            lines.Add(severalText);
            lines.Add(string.Empty);
            lines.AddRange(codes.Select(c => $"{F("Mail.CodeFor", c.Voter?.Name ?? member)}: {c.Code}"));
        }
        else
        {
            lines.Add(onBehalf is null ? null : F("Mail.OnBehalf", onBehalf));
            lines.Add(string.Empty);
            lines.Add($"{T("Mail.YourCode")}: {codes[0].Code}");
        }

        lines.AddRange([$"{T("Mail.VotingPage")}: {link}", string.Empty, keep, advance, string.Empty, secret, F("Mail.SignOff", club.ClubName)]);
        var text = string.Join("\n", lines.Where(static line => line is not null));

        static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
        // The texts are encoded before the (encoded) values go in, so a value can carry markup such as <strong>.
        string H(string key, params object?[] args) => string.Format(culture, E(T(key)), args);
        const string label = "margin:24px 0 4px;font-size:13px;text-transform:uppercase;letter-spacing:1px;color:#5A5784";
        var codeStyle = $"margin:0 0 20px;font-family:{E(club.HeadingFont)},Arial,sans-serif;font-size:34px;letter-spacing:4px";
        var codeBlocks = several
            ? string.Concat(codes.Select(c => $"""<p style="{label}">{H("Mail.CodeFor", E(c.Voter?.Name ?? member))}</p><p style="{codeStyle}">{E(c.Code)}</p>"""))
            : $"""<p style="{label}">{E(T("Mail.YourCode"))}</p><p style="{codeStyle}">{E(codes[0].Code)}</p>""";
        var locationHtml = meeting.Location is null ? string.Empty : H("Mail.Location", E(meeting.Location));
        var introHtml = H("Mail.Intro", $"<strong>{E(when)}</strong>", locationHtml, E(club.ClubName), E(meeting.Name))
                        + (onBehalf is null ? string.Empty : " " + H("Mail.OnBehalf", $"<strong>{E(onBehalf)}</strong>"));
        var html = $"""
            <!doctype html>
            <html lang="{E(culture.TwoLetterISOLanguageName)}"><body style="margin:0;background:#F4F5F9;font-family:{E(club.BodyFont)},Arial,sans-serif;color:#1F1F2E">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0"><tr><td align="center" style="padding:24px 12px">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;background:#FFFFFF;border-radius:12px;overflow:hidden">
            <tr><td style="background:{E(club.PrimaryColor)};color:#FFFFFF;padding:20px 24px;font-family:{E(club.HeadingFont)},Arial,sans-serif;font-size:22px;text-transform:uppercase;letter-spacing:1px">{E(meeting.Name)}</td></tr>
            <tr><td style="height:6px;background:{E(club.SecondaryColor)}"></td></tr>
            <tr><td style="padding:24px;font-size:16px;line-height:1.5">
            <p>{H("Mail.Greeting", E(greeting))}</p>
            <p>{introHtml}</p>
            {(several ? $"<p>{E(severalText)}</p>" : string.Empty)}
            {codeBlocks}
            <p><a href="{E(link)}" style="display:inline-block;background:{E(club.PrimaryColor)};color:#FFFFFF;text-decoration:none;padding:12px 22px;border-radius:8px;font-weight:600">{E(T("Mail.OpenVotingPage"))}</a></p>
            <p style="font-size:14px;color:#5A5784">{E(keep)}</p>
            <p style="font-size:14px;color:#5A5784">{E(advance)} {E(secret)}</p>
            </td></tr></table></td></tr></table></body></html>
            """;
        var from = $"{club.MailFromName} <{club.MailFromAddress}>";
        return new OutgoingMail(from, club.MailReplyTo, to, subject, text, html);
    }
}
