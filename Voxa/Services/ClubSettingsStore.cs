using Microsoft.EntityFrameworkCore;
using Voxa.Persistence.Entity;

namespace Voxa.Services;

/// <summary>Reads the club's settings row, or neutral defaults while the board has not saved any.</summary>
public sealed class ClubSettingsStore(VoxaContext context)
{
    public static readonly IReadOnlyList<string> Fonts = ["Oswald", "Hind", "Roboto", "Montserrat", "Open Sans", "Lato"];

    public static ClubSettings Defaults() => new()
    {
        ClubName = "Voxa",
        PrimaryColor = "#1F2A44",
        SecondaryColor = "#4FB3D9",
        HeadingFont = "Montserrat",
        BodyFont = "Open Sans",
        MailFromName = "Voxa",
        AdvanceVotingMaxDays = 30,
        MinorVoting = MinorVoting.Guardian,
        AdultAge = 18,
        MemberSource = MemberSourceKind.Csv,
    };

    public async Task<ClubSettings> GetAsync(CancellationToken cancellationToken) =>
        await context.ClubSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? Defaults();
}
