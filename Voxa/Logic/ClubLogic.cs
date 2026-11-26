using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Voxa.Abstractions;
using Voxa.Abstractions.Logic;
using Voxa.Abstractions.Models;
using Voxa.Persistence.Entity;
using Voxa.Services;

namespace Voxa.Logic;

public sealed class ClubLogic(VoxaContext context, ClubSettingsStore store, IOptions<KeevaOptions> keeva) : IClubLogic
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
            Language = club.Language,
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
            throw new DomainValidationException(Texts.Format("Error.FontInvalid", string.Join(", ", ClubSettingsStore.Fonts)));
        }

        var language = Mapping.Clean(request.Language) ?? Languages.Default;
        if (!Languages.IsSupported(language))
        {
            throw new DomainValidationException(Texts.Format("Error.LanguageInvalid", string.Join(", ", Languages.Supported)));
        }

        var logo = Mapping.Clean(request.LogoDataUrl);
        if (logo is not null && !s_logoTypes.Any(t => logo.StartsWith(t, StringComparison.Ordinal)))
        {
            throw new DomainValidationException(Texts.Get("Error.LogoType"));
        }

        if (logo?.Length > MaxLogoLength)
        {
            throw new DomainValidationException(Texts.Get("Error.LogoTooLarge"));
        }

        var club = await context.ClubSettings.FirstOrDefaultAsync(cancellationToken);
        if (club is null)
        {
            club = ClubSettingsStore.Defaults();
            _ = context.ClubSettings.Add(club);
        }

        club.ClubName = Mapping.Clean(request.ClubName) ?? throw new DomainValidationException(Texts.Get("Error.ClubNameRequired"));
        club.ContactEmail = Mapping.Clean(request.ContactEmail);
        club.Language = language;
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
        club.MemberSource = request.MemberSource;
        club.KeevaUrl = Mapping.Clean(request.KeevaUrl)?.TrimEnd('/');
        _ = await context.SaveChangesAsync(cancellationToken);
        return ToModel(club);
    }

    private ClubSettingsModel ToModel(ClubSettings club) => new()
    {
        ClubName = club.ClubName,
        ContactEmail = club.ContactEmail,
        Language = club.Language,
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
        MemberSource = club.MemberSource,
        KeevaUrl = club.KeevaUrl,
        KeevaKeyConfigured = !string.IsNullOrWhiteSpace(keeva.Value.ApiKey),
        AvailableFonts = ClubSettingsStore.Fonts,
    };
}
