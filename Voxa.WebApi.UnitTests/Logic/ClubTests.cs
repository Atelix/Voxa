using Microsoft.Extensions.Options;
using Voxa.Abstractions;
using Voxa.Abstractions.Models;
using Voxa.Logic;
using Voxa.Services;

namespace Voxa.WebApi.UnitTests.Logic;

public sealed class ClubTests
{
    [Fact]
    public async Task SettingsAreSavedAndTheBrandIsPublic()
    {
        using var services = new TestServices(DateTimeOffset.UtcNow);
        var logic = new ClubLogic(services.Context, services.Club, Options.Create(new KeevaOptions()));

        _ = await logic.UpdateAsync(Request("data:image/png;base64,AAAA"), CancellationToken.None);
        var brand = await logic.BrandAsync(CancellationToken.None);

        Assert.Equal("H.S.C. De Sittard Condors", brand.ClubName);
        Assert.Equal("#231E5B", brand.PrimaryColor);
        Assert.Equal("Oswald", brand.HeadingFont);
    }

    [Fact]
    public async Task ALogoThatIsTooLargeIsRefused()
    {
        using var services = new TestServices(DateTimeOffset.UtcNow);
        var logic = new ClubLogic(services.Context, services.Club, Options.Create(new KeevaOptions()));
        var logo = "data:image/png;base64," + new string('A', 1_000_000);

        _ = await Assert.ThrowsAsync<DomainValidationException>(() => logic.UpdateAsync(Request(logo), CancellationToken.None));
    }

    private static ClubSettingsRequest Request(string logo) => new()
    {
        ClubName = "H.S.C. De Sittard Condors",
        PrimaryColor = "#231e5b",
        SecondaryColor = "#62CDF6",
        HeadingFont = "Oswald",
        BodyFont = "Hind",
        LogoDataUrl = logo,
        MailFromName = "H.S.C. De Sittard Condors",
        AdvanceVotingMaxDays = 30,
    };
}
