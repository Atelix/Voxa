using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Voxa.Abstractions.Models;

namespace Voxa.WebApi.UnitTests.Api;

/// <summary>The API answers in the language the app asks for, and the club's language is public.</summary>
public sealed class LanguageApiTests(TestApi api) : IClassFixture<TestApi>
{
    [Theory]
    [InlineData("nl", "Het wachtwoord klopt niet.", "Ongeldig verzoek")]
    [InlineData("en-GB,en;q=0.9", "The password is wrong.", "Bad request")]
    [InlineData("de", "Het wachtwoord klopt niet.", "Ongeldig verzoek")]
    public async Task ErrorsFollowTheRequestedLanguage(string acceptLanguage, string detail, string title)
    {
        using var client = api.CreateClient();
        client.DefaultRequestHeaders.Add("X-Voxa-Device", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);

        using var response = await client.PostAsJsonAsync(new Uri("/api/admin/session", UriKind.Relative), new SignInRequest { Password = "fout" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(detail, body.RootElement.GetProperty("detail").GetString());
        Assert.Equal(title, body.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task TheBoardSetsTheClubsLanguage()
    {
        using var client = api.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en");
        using var signIn = await client.PostAsJsonAsync(new Uri("/api/admin/session", UriKind.Relative), new SignInRequest { Password = TestApi.Password });
        signIn.EnsureSuccessStatusCode();

        using var unknown = await client.PutAsJsonAsync(new Uri("/api/admin/club", UriKind.Relative), Settings("xx"));
        using var saved = await client.PutAsJsonAsync(new Uri("/api/admin/club", UriKind.Relative), Settings("en"));
        using var brand = await client.GetAsync(new Uri("/api/club", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Contains("Choose a language from: nl, en.", await unknown.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        saved.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await brand.Content.ReadAsStringAsync());
        Assert.Equal("en", body.RootElement.GetProperty("language").GetString());
    }

    private static ClubSettingsRequest Settings(string language) => new()
    {
        ClubName = "Club",
        Language = language,
        PrimaryColor = "#231E5B",
        SecondaryColor = "#62CDF6",
        HeadingFont = "Oswald",
        BodyFont = "Hind",
        MailFromName = "Club",
        AdvanceVotingMaxDays = 30,
    };
}
