using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Voxa.Abstractions.Services;

namespace Voxa.Services;

/// <summary>
/// Sends through the Mailgun HTTP API. Tracking is switched off, so Mailgun adds no click or open tracking to the code
/// mail. Logs never contain the recipient or the code.
/// </summary>
public sealed class MailgunMailSender(HttpClient http, IOptions<MailOptions> options, ILogger<MailgunMailSender> logger) : IMailSender
{
    public bool IsTrial => false;

    public async Task<bool> SendAsync(OutgoingMail mail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mail);
        var settings = options.Value;
        var fields = new List<KeyValuePair<string, string>>
        {
            new("from", mail.From),
            new("to", string.IsNullOrWhiteSpace(settings.RedirectTo) ? mail.To : settings.RedirectTo),
            new("subject", mail.Subject),
            new("text", mail.Text),
            new("html", mail.Html),
            new("o:tracking", "no"),
            new("o:tracking-clicks", "no"),
            new("o:tracking-opens", "no"),
        };
        if (!string.IsNullOrWhiteSpace(mail.ReplyTo))
        {
            fields.Add(new("h:Reply-To", mail.ReplyTo));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(settings.Mailgun.BaseUrl), $"{settings.Mailgun.Domain}/messages"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes("api:" + settings.Mailgun.ApiKey)));
        request.Content = new FormUrlEncodedContent(fields);
        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            logger.LogWarning("Mailgun refused a message with status {Status}.", (int)response.StatusCode);
            return false;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Mailgun could not be reached.");
            return false;
        }
    }
}
