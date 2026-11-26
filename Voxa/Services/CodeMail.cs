using System.Net;
using Voxa.Abstractions.Services;
using Voxa.Persistence.Entity;

namespace Voxa.Services;

/// <summary>The mail with a voting code, in Dutch and in the club's colours (design: the "E-mail met stemcode" mockup).</summary>
public static class CodeMail
{
    public static OutgoingMail Build(Meeting meeting, ClubSettings club, Voter? voter, string to, string code, AppOptions app)
    {
        ArgumentNullException.ThrowIfNull(meeting);
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(club);
        var link = $"{app.PublicUrl.TrimEnd('/')}/#code={code.Replace("-", string.Empty, StringComparison.Ordinal)}";
        var greeting = voter?.GuardianName ?? voter?.Name ?? "lid";
        var onBehalf = voter?.GuardianName is null ? null : voter.Name;
        var when = ClubTime.FormatDateTime(meeting.StartsAt);
        var advance = meeting.AdvanceVoting && meeting.AdvanceOpensAt is { } opens
            ? $"Vooraf stemmen kan vanaf {ClubTime.FormatDate(opens)} tot het begin van de vergadering."
            : "Stemmen kan tijdens de vergadering.";
        var subject = $"Je persoonlijke stemcode voor de {meeting.Name}";

        var text = string.Join("\n", new[]
        {
            $"Hoi {greeting},",
            string.Empty,
            $"Op {when}{(meeting.Location is null ? string.Empty : " in " + meeting.Location)} houdt {club.ClubName} de {meeting.Name}. Je kunt digitaal stemmen met je telefoon, vooraf of in de zaal.",
            onBehalf is null ? null : $"Met deze code stem je namens {onBehalf}.",
            string.Empty,
            $"Jouw stemcode: {code}",
            $"Stempagina: {link}",
            string.Empty,
            "Deze code is alleen voor jou. Deel hem niet: wie de code heeft, kan er één keer per stempunt mee stemmen.",
            "Ouder van meerdere leden? Dan krijg je per lid een eigen mail en code.",
            advance,
            string.Empty,
            "Je stem is geheim: niemand, ook het bestuur niet, kan zien wat jij hebt gestemd.",
            $"Groet, het bestuur van {club.ClubName}",
        }.Where(static line => line is not null));

        static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
        var html = $"""
            <!doctype html>
            <html lang="nl"><body style="margin:0;background:#F4F5F9;font-family:{E(club.BodyFont)},Arial,sans-serif;color:#1F1F2E">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0"><tr><td align="center" style="padding:24px 12px">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;background:#FFFFFF;border-radius:12px;overflow:hidden">
            <tr><td style="background:{E(club.PrimaryColor)};color:#FFFFFF;padding:20px 24px;font-family:{E(club.HeadingFont)},Arial,sans-serif;font-size:22px;text-transform:uppercase;letter-spacing:1px">{E(meeting.Name)}</td></tr>
            <tr><td style="height:6px;background:{E(club.SecondaryColor)}"></td></tr>
            <tr><td style="padding:24px;font-size:16px;line-height:1.5">
            <p>Hoi {E(greeting)},</p>
            <p>Op <strong>{E(when)}</strong>{(meeting.Location is null ? string.Empty : " in " + E(meeting.Location))} houdt {E(club.ClubName)} de {E(meeting.Name)}. Je kunt digitaal stemmen met je telefoon, vooraf of in de zaal.{(onBehalf is null ? string.Empty : $" Hiermee stem je namens <strong>{E(onBehalf)}</strong>.")}</p>
            <p style="margin:24px 0 4px;font-size:13px;text-transform:uppercase;letter-spacing:1px;color:#5A5784">Jouw stemcode</p>
            <p style="margin:0 0 20px;font-family:{E(club.HeadingFont)},Arial,sans-serif;font-size:34px;letter-spacing:4px">{E(code)}</p>
            <p><a href="{E(link)}" style="display:inline-block;background:{E(club.PrimaryColor)};color:#FFFFFF;text-decoration:none;padding:12px 22px;border-radius:8px;font-weight:600">Open de stempagina</a></p>
            <p style="font-size:14px;color:#5A5784">Deze code is alleen voor jou. Deel hem niet: wie de code heeft, kan er één keer per stempunt mee stemmen. Ouder van meerdere leden? Dan krijg je per lid een eigen mail en code.</p>
            <p style="font-size:14px;color:#5A5784">{E(advance)} Je stem is geheim: niemand, ook het bestuur niet, kan zien wat jij hebt gestemd.</p>
            </td></tr></table></td></tr></table></body></html>
            """;
        var from = $"{club.MailFromName} <{club.MailFromAddress}>";
        return new OutgoingMail(from, club.MailReplyTo, to, subject, text, html);
    }
}
