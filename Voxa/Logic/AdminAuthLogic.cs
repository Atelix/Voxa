using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Voxa.Abstractions;
using Voxa.Abstractions.Logic;
using Voxa.Abstractions.Models;
using Voxa.Abstractions.Services;
using Voxa.Services;

namespace Voxa.Logic;

public sealed class AdminOptions
{
    public const string Section = "Admin";

    /// <summary>The board's shared password. Required outside Development.</summary>
    public string? Password { get; set; }
}

/// <summary>Signs the board in with a cookie. Wrong passwords are limited per client like wrong codes.</summary>
public sealed class AdminAuthLogic(IHttpContextAccessor accessor, IOptions<AdminOptions> options, IAttemptLimiter limiter, IClientIdentity client, IMailSender mail) : IAdminAuthLogic
{
    public const string Role = "board";
    public const int MaxFailures = 5;

    public AdminSessionModel Status() => new()
    {
        SignedIn = accessor.HttpContext?.User.IsInRole(Role) == true,
        TrialMode = mail.IsTrial,
    };

    public async Task<AdminSessionModel> SignInAsync(SignInRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var keys = new[] { new AttemptKey("admin:" + client.Address, MaxFailures) };
        limiter.EnsureAllowed(keys);
        var expected = options.Value.Password;
        if (string.IsNullOrEmpty(expected) || !CryptographicOperations.FixedTimeEquals(Digest(request.Password), Digest(expected)))
        {
            limiter.RecordFailure(keys);
            throw new DomainValidationException(Texts.Get("Error.PasswordWrong"));
        }

        var context = accessor.HttpContext ?? throw new InvalidOperationException("Signing in needs a request.");
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "bestuur"), new Claim(ClaimTypes.Role, Role)], CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return new AdminSessionModel { SignedIn = true, TrialMode = mail.IsTrial };
    }

    public async Task<AdminSessionModel> SignOutAsync()
    {
        if (accessor.HttpContext is { } context)
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        return new AdminSessionModel { SignedIn = false, TrialMode = mail.IsTrial };
    }

    private static byte[] Digest(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
