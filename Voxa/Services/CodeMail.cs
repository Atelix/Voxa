using System.Net;
using Voxa.Abstractions.Services;
using Voxa.Persistence.Entity;

namespace Voxa.Services;

/// <summary>A code in a mail, with the voter it is for. The voter is only used for the text of the mail.</summary>
public sealed record MailedCode(Voter? Voter, string Code);

/// <summary>
/// The mail with voting codes, in Dutch and in the club's colours (design: the "E-mail met stemcode" mockup). An
/// address that serves several voters, such as a parent of several youth members, gets one mail with every code and
/// one link that opens the voting page with all of them.
/// </summary>
public static class CodeMail
{
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

        var several = codes.Count > 1;
        var link = $"{app.PublicUrl.TrimEnd('/')}/#code={string.Join(',', codes.Select(static c => c.Code.Replace("-", string.Empty, StringComparison.Ordinal)))}";
        var voters = codes.Select(static c => c.Voter).ToList();
        var greeting = voters.Select(static v => v?.GuardianName).FirstOrDefault(static g => g is not null) ?? voters[0]?.Name ?? "lid";
        var onBehalf = !several && voters[0]?.GuardianName is not null ? voters[0]!.Name : null;
        var when = ClubTime.FormatDateTime(meeting.StartsAt);
        var advance = meeting.AdvanceVoting && meeting.AdvanceOpensAt is { } opens
            ? $"Vooraf stemmen kan vanaf {ClubTime.FormatDate(opens)} tot het begin van de vergadering."
            : "Stemmen kan tijdens de vergadering.";
        var subject = several ? $"Je persoonlijke stemcodes voor de {meeting.Name}" : $"Je persoonlijke stemcode voor de {meeting.Name}";
        var intro = $"Op {when}{(meeting.Location is null ? string.Empty : " in " + meeting.Location)} houdt {club.ClubName} de {meeting.Name}. Je kunt digitaal stemmen met je telefoon, vooraf of in de zaal.";
        var severalText = $"Dit adres hoort bij {codes.Count} stemgerechtigde leden, dus je krijgt {codes.Count} codes: elke code is de stem van één lid. Met de knop open je de stempagina met alle codes. Je stemt dan voor alle leden tegelijk, of per lid apart.";
        var keep = several
            ? "Deze codes zijn alleen voor jou. Deel ze niet: wie een code heeft, kan er één keer per stempunt mee stemmen."
            : "Deze code is alleen voor jou. Deel hem niet: wie de code heeft, kan er één keer per stempunt mee stemmen.";
        var secret = "Je stem is geheim: niemand, ook het bestuur niet, kan zien wat jij hebt gestemd.";

        var lines = new List<string?> { $"Hoi {greeting},", string.Empty, intro };
        if (several)
        {
            lines.Add(severalText);
            lines.Add(string.Empty);
            lines.AddRange(codes.Select(static c => $"Stemcode voor {c.Voter?.Name ?? "lid"}: {c.Code}"));
        }
        else
        {
            lines.Add(onBehalf is null ? null : $"Met deze code stem je namens {onBehalf}.");
            lines.Add(string.Empty);
            lines.Add($"Jouw stemcode: {codes[0].Code}");
        }

        lines.AddRange([$"Stempagina: {link}", string.Empty, keep, advance, string.Empty, secret, $"Groet, het bestuur van {club.ClubName}"]);
        var text = string.Join("\n", lines.Where(static line => line is not null));

        static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
        const string label = "margin:24px 0 4px;font-size:13px;text-transform:uppercase;letter-spacing:1px;color:#5A5784";
        var codeStyle = $"margin:0 0 20px;font-family:{E(club.HeadingFont)},Arial,sans-serif;font-size:34px;letter-spacing:4px";
        var codeBlocks = several
            ? string.Concat(codes.Select(c => $"""<p style="{label}">Stemcode voor {E(c.Voter?.Name ?? "lid")}</p><p style="{codeStyle}">{E(c.Code)}</p>"""))
            : $"""<p style="{label}">Jouw stemcode</p><p style="{codeStyle}">{E(codes[0].Code)}</p>""";
        var introHtml = $"Op <strong>{E(when)}</strong>{(meeting.Location is null ? string.Empty : " in " + E(meeting.Location))} houdt {E(club.ClubName)} de {E(meeting.Name)}. Je kunt digitaal stemmen met je telefoon, vooraf of in de zaal."
                        + (onBehalf is null ? string.Empty : $" Hiermee stem je namens <strong>{E(onBehalf)}</strong>.");
        var html = $"""
            <!doctype html>
            <html lang="nl"><body style="margin:0;background:#F4F5F9;font-family:{E(club.BodyFont)},Arial,sans-serif;color:#1F1F2E">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0"><tr><td align="center" style="padding:24px 12px">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;background:#FFFFFF;border-radius:12px;overflow:hidden">
            <tr><td style="background:{E(club.PrimaryColor)};color:#FFFFFF;padding:20px 24px;font-family:{E(club.HeadingFont)},Arial,sans-serif;font-size:22px;text-transform:uppercase;letter-spacing:1px">{E(meeting.Name)}</td></tr>
            <tr><td style="height:6px;background:{E(club.SecondaryColor)}"></td></tr>
            <tr><td style="padding:24px;font-size:16px;line-height:1.5">
            <p>Hoi {E(greeting)},</p>
            <p>{introHtml}</p>
            {(several ? $"<p>{E(severalText)}</p>" : string.Empty)}
            {codeBlocks}
            <p><a href="{E(link)}" style="display:inline-block;background:{E(club.PrimaryColor)};color:#FFFFFF;text-decoration:none;padding:12px 22px;border-radius:8px;font-weight:600">Open de stempagina</a></p>
            <p style="font-size:14px;color:#5A5784">{E(keep)}</p>
            <p style="font-size:14px;color:#5A5784">{E(advance)} {E(secret)}</p>
            </td></tr></table></td></tr></table></body></html>
            """;
        var from = $"{club.MailFromName} <{club.MailFromAddress}>";
        return new OutgoingMail(from, club.MailReplyTo, to, subject, text, html);
    }
}
