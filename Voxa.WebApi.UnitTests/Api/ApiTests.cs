using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Voxa.Abstractions.Models;

namespace Voxa.WebApi.UnitTests.Api;

public sealed class ApiTests(TestApi api) : IClassFixture<TestApi>
{
    private readonly HttpClient _client = api.CreateClient();

    [Fact]
    public async Task HealthReportsTheVersion()
    {
        using var response = await _client.GetAsync(new Uri("/api/health", UriKind.Relative));

        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("1.2.3", body.RootElement.GetProperty("version").GetString());
        Assert.Equal("1.2.3", response.Headers.GetValues(AppVersion.HeaderName).Single());
    }

    [Fact]
    public async Task TheBoardPagesNeedASignIn()
    {
        using var client = api.CreateClient();
        using var response = await client.GetAsync(new Uri("/api/admin/meetings", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AWrongPasswordIsRefusedAndTheRightOneSignsIn()
    {
        using var client = api.CreateClient();
        using var wrong = await client.PostAsJsonAsync(new Uri("/api/admin/session", UriKind.Relative), new SignInRequest { Password = "fout" });
        using var right = await client.PostAsJsonAsync(new Uri("/api/admin/session", UriKind.Relative), new SignInRequest { Password = TestApi.Password });
        using var list = await client.GetAsync(new Uri("/api/admin/meetings", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        right.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
    }

    [Fact]
    public async Task TheClubBrandIsPublicAndEnumsAreStrings()
    {
        using var brand = await _client.GetAsync(new Uri("/api/club", UriKind.Relative));
        brand.EnsureSuccessStatusCode();

        using var client = api.CreateClient();
        using var signIn = await client.PostAsJsonAsync(new Uri("/api/admin/session", UriKind.Relative), new SignInRequest { Password = TestApi.Password });
        using var settings = await client.GetAsync(new Uri("/api/admin/club", UriKind.Relative));
        using var body = JsonDocument.Parse(await settings.Content.ReadAsStringAsync());
        Assert.Equal("Guardian", body.RootElement.GetProperty("minorVoting").GetString());
    }

    [Fact]
    public async Task AnUnknownCodeIsNotFoundAndTooManyAreRateLimited()
    {
        using var client = api.CreateClient();
        client.DefaultRequestHeaders.Add("X-Voxa-Device", Guid.NewGuid().ToString());
        for (var i = 0; i < 5; i++)
        {
            using var unknown = await client.PostAsJsonAsync(new Uri("/api/ballot/session", UriKind.Relative), new BallotCodeRequest { Code = "AAAA-AAAA" });
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        }

        using var limited = await client.PostAsJsonAsync(new Uri("/api/ballot/session", UriKind.Relative), new BallotCodeRequest { Code = "AAAA-AAAA" });

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.True(limited.Headers.RetryAfter is not null);
    }
}
