using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Voxa.Abstractions.Services;
using Voxa.Services;

namespace Voxa.WebApi;

/// <summary>
/// Signs in a member administration that sends <c>Authorization: Bearer &lt;Import__ApiKey&gt;</c>. It only reaches the
/// import API (<c>/api/import</c>), never the board's pages, and the board's cookie does not reach the import API.
/// Wrong keys are limited per client address like wrong board passwords.
/// </summary>
public sealed class ImportApiKeyHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<ImportOptions> import,
    IAttemptLimiter limiter,
    IClientIdentity client) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ImportApiKey";
    public const string Role = "import";
    public const int MaxFailures = 10;

    private const string BearerPrefix = "Bearer ";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var keys = new[] { new AttemptKey("import:" + client.Address, MaxFailures) };
        limiter.EnsureAllowed(keys);
        var expected = import.Value.ApiKey;
        var given = header[BearerPrefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(expected) || !CryptographicOperations.FixedTimeEquals(Digest(given), Digest(expected)))
        {
            limiter.RecordFailure(keys);
            return Task.FromResult(AuthenticateResult.Fail("The import API key is wrong."));
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "import"), new Claim(ClaimTypes.Role, Role)], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }

    private static byte[] Digest(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
