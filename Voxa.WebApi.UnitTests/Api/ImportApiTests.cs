using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Voxa.Abstractions.Models;

namespace Voxa.WebApi.UnitTests.Api;

/// <summary>The member import API: only the import key reaches it, and the key reaches nothing else.</summary>
public sealed class ImportApiTests(TestApi api) : IClassFixture<TestApi>
{
    private static readonly Uri s_meetings = new("/api/import/meetings", UriKind.Relative);

    [Fact]
    public async Task WithoutTheKeyTheImportApiIsRefused()
    {
        using var client = api.CreateClient();
        client.DefaultRequestHeaders.Add("CF-Connecting-IP", "198.51.100.1");
        using var none = await client.GetAsync(s_meetings);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "wrong-key");
        using var wrong = await client.GetAsync(s_meetings);

        Assert.Equal(HttpStatusCode.Unauthorized, none.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
    }

    [Fact]
    public async Task TooManyWrongKeysAreRateLimited()
    {
        using var client = api.CreateClient();
        client.DefaultRequestHeaders.Add("CF-Connecting-IP", "198.51.100.2");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "wrong-key");
        for (var i = 0; i < 10; i++)
        {
            using var wrong = await client.GetAsync(s_meetings);
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        }

        using var limited = await client.GetAsync(s_meetings);

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }

    [Fact]
    public async Task TheBoardsSignInDoesNotReachTheImportApiAndTheKeyDoesNotReachTheBoard()
    {
        using var board = api.CreateClient();
        using var signIn = await board.PostAsJsonAsync(new Uri("/api/admin/session", UriKind.Relative), new SignInRequest { Password = TestApi.Password });
        signIn.EnsureSuccessStatusCode();
        using var boardImport = await board.GetAsync(s_meetings);

        using var import = ImportClient();
        using var importBoard = await import.GetAsync(new Uri("/api/admin/meetings", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, boardImport.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, importBoard.StatusCode);
    }

    [Fact]
    public async Task TheKeySendsTheMemberListToAMeeting()
    {
        using var board = api.CreateClient();
        using var signIn = await board.PostAsJsonAsync(new Uri("/api/admin/session", UriKind.Relative), new SignInRequest { Password = TestApi.Password });
        using var created = await board.PostAsJsonAsync(new Uri("/api/admin/meetings", UriKind.Relative), new MeetingRequest { Name = "ALV import", StartsAt = DateTime.UtcNow.AddDays(20) });
        var meetingId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var import = ImportClient();

        var targets = await import.GetFromJsonAsync<JsonElement>(s_meetings);
        // The wire format a member administration sends, with a date of birth as yyyy-MM-dd and enums as strings.
        using var body = new StringContent("""
            {"members": [
              {"name": "Anna Adult", "email": "anna@example.org", "birthDate": "1990-01-01"},
              {"name": "Kees Kind", "birthDate": "2020-05-31", "guardians": [{"name": "Petra Ouder", "email": "petra@example.org"}]},
              {"name": "Gerda Geschorst", "email": "gerda@example.org", "suspended": true}
            ]}
            """, System.Text.Encoding.UTF8, "application/json");
        using var response = await import.PostAsync(new Uri($"/api/import/meetings/{meetingId}/members", UriKind.Relative), body);
        using var invalid = await import.PostAsJsonAsync(new Uri($"/api/import/meetings/{meetingId}/members", UriKind.Relative), new { members = Array.Empty<object>() });
        using var missing = await import.PostAsJsonAsync(new Uri($"/api/import/meetings/{Guid.NewGuid()}/members", UriKind.Relative), new { members = new[] { new { name = "Anna" } } });

        Assert.Contains(targets.EnumerateArray(), t => t.GetProperty("id").GetGuid() == meetingId && t.GetProperty("importMode").GetString() == "Replaced");
        response.EnsureSuccessStatusCode();
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Replaced", result.RootElement.GetProperty("mode").GetString());
        Assert.Equal(2, result.RootElement.GetProperty("imported").GetInt32());
        Assert.Equal(1, result.RootElement.GetProperty("viaGuardian").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task TheImportContractIsPublishedOnItsOwn()
    {
        using var client = api.CreateClient();
        using var response = await client.GetAsync(new Uri(ImportApi.DocumentPath, UriKind.Relative));

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths").EnumerateObject().Select(static p => p.Name).ToList();
        Assert.Equal(["/api/import/meetings", "/api/import/meetings/{meetingId}/members"], paths);
        Assert.Equal("bearer", document.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("importKey").GetProperty("scheme").GetString());
    }

    private HttpClient ImportClient()
    {
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestApi.ImportKey);
        return client;
    }
}
