using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Voxa.Abstractions;
using Voxa.Abstractions.Logic;
using Voxa.Abstractions.Models;
using Voxa.Persistence.Entity;
using Voxa.Services;

namespace Voxa.Logic;

public sealed class ClubLogic(VoxaContext context, ClubSettingsStore store, IOptions<ImportOptions> import) : IClubLogic
{
    /// <summary>A data URL of a 700 kB image, as base64 adds a third.</summary>
    private const int MaxLogoLength = 960_000;

    private static readonly string[] s_logoTypes = ["data:image/png;base64,", "data:image/jpeg;base64,", "data:image/svg+xml;base64,", "data:image/webp;base64,"];

    public async Task<ClubBrandModel> BrandAsync(CancellationToken cancellationToken)
    {
        var club = await store.GetAsync(cancellationToken);
        return new ClubBrandModel
        {
            ClubName = club.ClubName,
            ContactEmail = club.ContactEmail,
            PrimaryColor = club.PrimaryColor,
            SecondaryColor = club.SecondaryColor,
            HeadingFont = club.HeadingFont,
            BodyFont = club.BodyFont,
            LogoDataUrl = club.LogoDataUrl,
        };
    }

    public async Task<ClubSettingsModel> GetAsync(CancellationToken cancellationToken) => ToModel(await store.GetAsync(cancellationToken));

    public async Task<ClubSettingsModel> UpdateAsync(ClubSettingsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!ClubSettingsStore.Fonts.Contains(request.HeadingFont) || !ClubSettingsStore.Fonts.Contains(request.BodyFont))
        {
            throw new DomainValidationException($"Choose a font from: {string.Join(", ", ClubSettingsStore.Fonts)}.");
        }

        var logo = Mapping.Clean(request.LogoDataUrl);
        if (logo is not null && !s_logoTypes.Any(t => logo.StartsWith(t, StringComparison.Ordinal)))
        {
            throw new DomainValidationException("The logo must be a PNG, JPEG, SVG or WebP image.");
        }

        if (logo?.Length > MaxLogoLength)
        {
            throw new DomainValidationException("The logo is too large; keep it under 700 kB.");
        }

        var club = await context.ClubSettings.FirstOrDefaultAsync(cancellationToken);
        if (club is null)
        {
            club = ClubSettingsStore.Defaults();
            _ = context.ClubSettings.Add(club);
        }

        club.ClubName = Mapping.Clean(request.ClubName) ?? throw new DomainValidationException("The club needs a name.");
        club.ContactEmail = Mapping.Clean(request.ContactEmail);
        club.PrimaryColor = request.PrimaryColor.ToUpperInvariant();
        club.SecondaryColor = request.SecondaryColor.ToUpperInvariant();
        club.HeadingFont = request.HeadingFont;
        club.BodyFont = request.BodyFont;
        club.LogoDataUrl = logo;
        club.MailFromName = Mapping.Clean(request.MailFromName) ?? club.ClubName;
        club.MailFromAddress = Mapping.Clean(request.MailFromAddress);
        club.MailReplyTo = Mapping.Clean(request.MailReplyTo);
        club.AdvanceVotingMaxDays = request.AdvanceVotingMaxDays;
        club.AdvanceVotingArticle = Mapping.Clean(request.AdvanceVotingArticle);
        club.MinorVoting = request.MinorVoting;
        club.AdultAge = request.AdultAge;
        _ = await context.SaveChangesAsync(cancellationToken);
        return ToModel(club);
    }

    private ClubSettingsModel ToModel(ClubSettings club) => new()
    {
        ClubName = club.ClubName,
        ContactEmail = club.ContactEmail,
        PrimaryColor = club.PrimaryColor,
        SecondaryColor = club.SecondaryColor,
        HeadingFont = club.HeadingFont,
        BodyFont = club.BodyFont,
        LogoDataUrl = club.LogoDataUrl,
        MailFromName = club.MailFromName,
        MailFromAddress = club.MailFromAddress,
        MailReplyTo = club.MailReplyTo,
        AdvanceVotingMaxDays = club.AdvanceVotingMaxDays,
        AdvanceVotingArticle = club.AdvanceVotingArticle,
        MinorVoting = club.MinorVoting,
        AdultAge = club.AdultAge,
        ImportApiConfigured = import.Value.IsConfigured,
        AvailableFonts = ClubSettingsStore.Fonts,
    };
}
